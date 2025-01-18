const RtcApi = {
    connectAnswerCallback: null,
    CallConnectAnswer: function (managedPtr, answerJson) {
        console.log("RtcApi: CallConnectAnswer:", managedPtr, answerJson);
        const ptr = stringToNewUTF8(answerJson);
        {{{ makeDynCall('vii', 'RtcApi.connectAnswerCallback') }}}(managedPtr, ptr);
        _free(ptr);
    },
    connectCandidatesCallback: null,
    CallConnectCandidates: function (managedPtr, candidates) {
        console.log("RtcApi: CallConnectCandidates:", managedPtr, candidates);
        const candidateJson = JSON.stringify(candidates)
        const ptr = stringToNewUTF8(candidateJson)
        {{{ makeDynCall('vii', 'RtcApi.connectCandidatesCallback') }}}(managedPtr, ptr)
        _free(ptr)
    },
    connectCompleteCallback: null,
    CallConnectComplete: function (managedPtr, error) {
        console.log("RtcApi: CallConnectComplete:", managedPtr, error);
        if (error) {
            const ptr = stringToNewUTF8(error);
            {{{ makeDynCall('vii', 'RtcApi.connectCompleteCallback') }}}(managedPtr, ptr);
            _free(ptr);
        } else {
            {{{ makeDynCall('vii', 'RtcApi.connectCompleteCallback') }}}(managedPtr, null);
        }
    },
    receivedCallback: null,
    CallReceived: async function (managedPtr, data) {
        //console.log("RtcApi: CallReceived:", managedPtr, typeof data, data);
        if (data) {
            if (data.constructor === String) {
                // const ptr = stringToNewUTF8(data);
                // const size = new TextEncoder().encode(data).length;
                // {{{ makeDynCall('vii', 'RtcApi.receivedCallback') }}}(managedPtr, ptr, size);
                // _free(ptr);
                // return;
                data = new TextEncoder().encode(data); //Uint8Array
            } else if (data instanceof ArrayBuffer) {
                data = new Uint8Array(data);
            } else if (data instanceof Blob) {
                const buffer = await data.arrayBuffer(); //ArrayBuffer
                data = new Uint8Array(buffer);
            } else {
                console.log("RtcApi: CallReceived: TODO: handle unsupported yet data type:", data);
                return;
            }
            
            const ptr = _malloc(data.byteLength);
            HEAPU8.set(data, ptr);
            {{{ makeDynCall('viii', 'RtcApi.receivedCallback') }}}(managedPtr, ptr, data.byteLength);
            _free(ptr);
        } else {
            {{{ makeDynCall('viii', 'RtcApi.receivedCallback') }}}(managedPtr, null, 0);
        }
    },

    peers: [],
    next: 0,

    AddNextPeer: function (peer) {
        const nativeHandle = RtcApi.next;
        RtcApi.next++;
        peer.nativeHandle = nativeHandle;
        RtcApi.peers[nativeHandle] = peer;
        return nativeHandle;
    },
    GetPeer: function (nativeHandle) {
        return RtcApi.peers[nativeHandle];
    },
    RemovePeer: function (nativeHandle) {
        RtcApi.peers[nativeHandle] = undefined;
    },
}

function RtcInit(connectAnswerCallback, connectCandidatesCallback, connectCompleteCallback, receivedCallback) {
    console.log("RtcInit:", 
        connectAnswerCallback, 
        connectCandidatesCallback, 
        connectCompleteCallback,
        receivedCallback);
    RtcApi.connectAnswerCallback = connectAnswerCallback;
    RtcApi.connectCandidatesCallback = connectCandidatesCallback;
    RtcApi.connectCompleteCallback = connectCompleteCallback;
    RtcApi.receivedCallback = receivedCallback;
}

function RtcConnect(managedPtr, offerPtr) {
    const offerStr = UTF8ToString(offerPtr);
    const offer = JSON.parse(offerStr);
    console.log("RtcConnect: offer:", offer);

    //const STUN_URL = "stun:stun.sipsorcery.com";
    //pc = new RTCPeerConnection({ iceServers: [{ urls: STUN_URL }] });
    pc = new RTCPeerConnection();
    pc.managedPtr = managedPtr;
    const nativeHandle = RtcApi.AddNextPeer(pc);
    
    pc.onconnectionstatechange = (event) => {
        console.log("RtcConnect: onconnectionstatechange:", pc.connectionState, event);
    }

    const iceCandidates = []
    pc.onicecandidate = async function (event) {
        const candidate = event.candidate;
        console.log("RtcConnect: onicecandidate:", candidate);
        if (candidate && candidate.candidate) {
            iceCandidates.push(candidate);
        }
    };
    pc.onicegatheringstatechange = async function () {
        const iceGatheringState = pc.iceGatheringState;
        console.log("RtcConnect: onicegatheringstatechange:", iceGatheringState);
        if (iceGatheringState === "complete") {
            console.log("RtcConnect: onicegatheringstatechange: posting local candidates:", iceCandidates);
            RtcApi.CallConnectCandidates(managedPtr, iceCandidates);
        }
    }

    pc.oniceconnectionstatechange = function () {
        console.log("RtcConnect: oniceconnectionstatechange:", pc.iceConnectionState);
    }
    pc.onsignalingstatechange = function () {
        console.log("RtcConnect: onsignalingstatechange:", pc.signalingState);
    }
    pc.onicecandidateerror = function (event) {
        console.log("RtcConnect: onicecandidateerror:", event);
    }

    pc.ondatachannel = (event) => {
        const channel = event.channel
        console.log("RtcConnect: ondatachannel:", channel);
        pc.mainChannel = channel;
        RtcApi.CallConnectComplete(managedPtr, null);
        channel.onmessage = function (event) {
            //console.log("RtcConnect: onmessage:", event.data);
            RtcApi.CallReceived(managedPtr, event.data);
        }
    }

    pc.setRemoteDescription(offer).then(async () => {
        console.log("RtcConnect: creating answer");
        const answer = await pc.createAnswer();
        console.log("RtcConnect: assign answer:", answer);
        await pc.setLocalDescription(answer);
        RtcApi.CallConnectAnswer(managedPtr, JSON.stringify(answer));
    }).catch((e) => {
        RtcApi.CallConnectComplete(managedPtr, e.message);
    });

    console.log("RtcConnect: nativeHandle:", nativeHandle);
    return nativeHandle;
}

function RtcAddIceCandidate(nativeHandle, candidateJsonPtr) {
    const candidateJson = UTF8ToString(candidateJsonPtr);
    console.log("RtcAddIceCandidate:", nativeHandle, candidateJson);
    const pc = RtcApi.GetPeer(nativeHandle);
    if (pc) {
        let candidate = new RTCIceCandidate(JSON.parse(candidateJson));
        console.log("RtcAddIceCandidate:", nativeHandle, candidate);
        pc.addIceCandidate(candidate).catch((e) => {
            console.log("RtcAddIceCandidate: addIceCandidate: failed:", nativeHandle, candidate, e);
        });
    } else {
        //TODO: to get rid of this warning if session is already closed make fetch('setanswer') cancellable via `AbortController`
        console.warn("RtcAddIceCandidate: failed to find peer", nativeHandle);
    }
}

function RtcClose(nativeHandle) {
    console.log("RtcClose: nativeHandle:", nativeHandle);
    const pc = RtcApi.GetPeer(nativeHandle);
    if (pc) {
        pc.close();
        RtcApi.RemovePeer(nativeHandle);
    } else {
        console.warn("RtcClose: failed to find peer", nativeHandle);
    }
}

function RtcSend(nativeHandle, bytes, size) {
    const pc = RtcApi.GetPeer(nativeHandle);
    if (pc) {
        const channel = pc.mainChannel;
        if (channel) {
            //console.log("RtcSend:", nativeHandle, bytes, size);
            const data = new Uint8Array(HEAPU8.buffer, bytes, size);
            channel.send(data);
        } else {
            console.warn("RtcSend: failed to obtain channel", nativeHandle, size, bytes);
        }
    } else {
        console.warn("RtcSend: failed to find peer", nativeHandle, size, bytes);
    }
}

const RtcApiLib = {
    $RtcApi: RtcApi,
    RtcInit,
    RtcConnect,
    RtcAddIceCandidate,
    RtcClose,
    RtcSend,
};

autoAddDeps(RtcApiLib, "$RtcApi");
mergeInto(LibraryManager.library, RtcApiLib);
 