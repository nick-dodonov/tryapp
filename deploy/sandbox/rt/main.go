// SPDX-FileCopyrightText: 2023 The Pion community <https://pion.ly>
// SPDX-License-Identifier: MIT

//go:build !js
// +build !js

// ortc demonstrates Pion WebRTC's ORTC capabilities.
package main

import (
	"bufio"
	"bytes"
	"compress/zlib"
	"encoding/base64"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"io"
	"net/http"
	"os"
	"strconv"
	"strings"
	"time"

	"github.com/pion/randutil"
	"github.com/pion/webrtc/v4"
)

// nolint:cyclop
func main() {
	isOffer := flag.Bool("offer", false, "Act as the offerer if set")
	port := flag.Int("port", 8080, "http server port (only for offerer)")
	stun := flag.String("stun", "stun:stun.l.google.com:19302", "STUN address for offerer and answerer (must be the same)")
	flag.Parse()

	fmt.Printf("#### OFFER: %t\n", *isOffer)
	if *isOffer {
		fmt.Printf("#### PORT: %d\n", *port)
	}
	fmt.Printf("#### STUN: %s\n", *stun)

	// Everything below is the Pion WebRTC (ORTC) API! Thanks for using it ❤️.

	// Prepare ICE gathering options
	iceOptions := webrtc.ICEGatherOptions{
		ICEServers: []webrtc.ICEServer{
			{URLs: []string{*stun}},
		},
	}

	// Create an API object
	api := webrtc.NewAPI()

	// Create the ICE gatherer
	gatherer, err := api.NewICEGatherer(iceOptions)
	if err != nil {
		panic(err)
	}

	// Construct the ICE transport
	ice := api.NewICETransport(gatherer)

	// Construct the DTLS transport
	dtls, err := api.NewDTLSTransport(ice, nil)
	if err != nil {
		panic(err)
	}

	// Construct the SCTP transport
	sctp := api.NewSCTPTransport(dtls)

	// Handle incoming data channels
	sctp.OnDataChannel(func(channel *webrtc.DataChannel) {
		fmt.Printf("New DataChannel %s %d\n", channel.Label(), channel.ID())

		// Register the handlers
		channel.OnOpen(handleOnOpen(channel))
		channel.OnMessage(func(msg webrtc.DataChannelMessage) {
			fmt.Printf("Message from DataChannel '%s': '%s'\n", channel.Label(), string(msg.Data))
		})
	})

	gatherFinished := make(chan struct{})
	gatherer.OnLocalCandidate(func(candidate *webrtc.ICECandidate) {
		if candidate == nil {
			close(gatherFinished)
		}
	})

	// Gather candidates
	if err = gatherer.Gather(); err != nil {
		panic(err)
	}

	<-gatherFinished

	iceCandidates, err := gatherer.GetLocalCandidates()
	if err != nil {
		panic(err)
	}

	iceParams, err := gatherer.GetLocalParameters()
	if err != nil {
		panic(err)
	}

	dtlsParams, err := dtls.GetLocalParameters()
	if err != nil {
		panic(err)
	}

	sctpCapabilities := sctp.GetCapabilities()

	s := Signal{
		ICECandidates:    iceCandidates,
		ICEParameters:    iceParams,
		DTLSParameters:   dtlsParams,
		SCTPCapabilities: sctpCapabilities,
	}

	iceRole := webrtc.ICERoleControlled

	// Exchange the information
	if *isOffer {
		printSignal(s, "Offer to send")
	} else {
		printSignal(s, "Answer to send")
	}
	fmt.Println(encode(s))
	remoteSignal := Signal{}

	if *isOffer {
		signalingChan := httpSDPServer(*port)
		decode(<-signalingChan, &remoteSignal)
		printSignal(remoteSignal, "Received answer")

		iceRole = webrtc.ICERoleControlling
	} else {
		fmt.Printf("#### Awaiting offer\n")
		decode(readUntilNewline(), &remoteSignal)
		printSignal(remoteSignal, "Received offer")
	}

	if err = ice.SetRemoteCandidates(remoteSignal.ICECandidates); err != nil {
		panic(err)
	}

	// Start the ICE transport
	fmt.Printf("#### Starting ICE transport\n")
	err = ice.Start(nil, remoteSignal.ICEParameters, &iceRole)
	if err != nil {
		panic(err)
	}

	// Start the DTLS transport
	fmt.Printf("#### Starting DTLS transport\n")
	if err = dtls.Start(remoteSignal.DTLSParameters); err != nil {
		panic(err)
	}

	// Start the SCTP transport
	fmt.Printf("#### Starting SCTP transport\n")
	if err = sctp.Start(remoteSignal.SCTPCapabilities); err != nil {
		panic(err)
	}

	// Construct the data channel as the offerer
	if *isOffer {
		fmt.Printf("#### Offer creating data channel\n")

		var id uint16 = 1

		dcParams := &webrtc.DataChannelParameters{
			Label: "Foo",
			ID:    &id,
		}
		var channel *webrtc.DataChannel
		channel, err = api.NewDataChannel(sctp, dcParams)
		if err != nil {
			panic(err)
		}

		// Register the handlers
		// channel.OnOpen(handleOnOpen(channel)) // TODO: OnOpen on handle ChannelAck
		go handleOnOpen(channel)() // Temporary alternative
		channel.OnMessage(func(msg webrtc.DataChannelMessage) {
			fmt.Printf("Message from DataChannel '%s': '%s'\n", channel.Label(), string(msg.Data))
		})
	} else {
		fmt.Printf("#### Answer awaiting data channel\n")
	}

	select {}
}

// Signal is used to exchange signaling info.
// This is not part of the ORTC spec. You are free
// to exchange this information any way you want.
type Signal struct {
	ICECandidates    []webrtc.ICECandidate   `json:"iceCandidates"`
	ICEParameters    webrtc.ICEParameters    `json:"iceParameters"`
	DTLSParameters   webrtc.DTLSParameters   `json:"dtlsParameters"`
	SCTPCapabilities webrtc.SCTPCapabilities `json:"sctpCapabilities"`
}

func handleOnOpen(channel *webrtc.DataChannel) func() {
	return func() {
		fmt.Printf(
			"Data channel '%s'-'%d' open. Random messages will now be sent to any connected DataChannels every 5 seconds\n",
			channel.Label(), channel.ID(),
		)

		ticker := time.NewTicker(5 * time.Second)
		defer ticker.Stop()
		for range ticker.C {
			message, err := randutil.GenerateCryptoRandomString(15, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")
			if err != nil {
				panic(err)
			}

			fmt.Printf("Sending %s \n", message)
			if err := channel.SendText(message); err != nil {
				panic(err)
			}
		}
	}
}

// Read from stdin until we get a newline.
func readUntilNewline() (in string) {
	var err error

	r := bufio.NewReader(os.Stdin)
	for {
		in, err = r.ReadString('\n')
		if err != nil && !errors.Is(err, io.EOF) {
			panic(err)
		}

		if in = strings.TrimSpace(in); len(in) > 0 {
			break
		}
	}

	fmt.Println("")

	return
}

// JSON encode + base64 a SessionDescription.
func encode(obj Signal) string {
	b, err := json.Marshal(obj)
	if err != nil {
		panic(err)
	}

    //** workaround the issue with long copy/paste on macOS
	var cb bytes.Buffer
	w := zlib.NewWriter(&cb)
	w.Write(b)
	w.Close()

	return base64.StdEncoding.EncodeToString(cb.Bytes())
}

func printSignal(obj Signal, logPrefix string) {
	b, err := json.MarshalIndent(obj, "", "  ")
	if err != nil {
		panic(err)
	}

	fmt.Printf(">>>> %s\n", logPrefix)
	fmt.Printf("%s\n", b)
	fmt.Printf("<<<< %s\n", logPrefix)
}

// Decode a base64 and unmarshal JSON into a SessionDescription.
func decode(in string, obj *Signal) {
	b, err := base64.StdEncoding.DecodeString(in)
	if err != nil {
		panic(err)
	}

	br := bytes.NewReader(b)
	z, err := zlib.NewReader(br)
    if err != nil {
        panic(err)
    }
	defer z.Close()
	ub, err := io.ReadAll(z)
    if err != nil {
        panic(err)
    }

	if err = json.Unmarshal(ub, obj); err != nil {
		panic(err)
	}
}

// httpSDPServer starts a HTTP Server that consumes SDPs.
func httpSDPServer(port int) chan string {
	fmt.Printf("#### Awaiting answer\n")
	sdpChan := make(chan string)
	http.HandleFunc("/", func(res http.ResponseWriter, req *http.Request) {
		body, _ := io.ReadAll(req.Body)
		fmt.Fprintf(res, "done") //nolint: errcheck
		fmt.Printf("#### Received answer: http\n")
		sdpChan <- string(body)
	})

	go func() {
		// nolint: gosec
		panic(http.ListenAndServe(":"+strconv.Itoa(port), nil))
	}()

	go func() {
		answer := readUntilNewline()
		fmt.Printf("#### Received answer: cli\n")
		sdpChan <- answer
	}()

	return sdpChan
}
