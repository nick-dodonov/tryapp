const RtcApi = {
    connectAnswerCallback: null,
    CallConnectAnswer: function(peerId, answerJson) {
        console.log("RtcApi: CallConnectAnswer:", peerId, answerJson);
        const ptr = stringToNewUTF8(answerJson);
        {{{ makeDynCall('vii', 'RtcApi.connectAnswerCallback') }}}(peerId, ptr);
        _free(ptr);
    },
    connectCompleteCallback: null,
    CallConnectComplete: function(peerId, error) {
        console.log("RtcApi: CallConnectComplete:", peerId, error);
        if (error) {
            const ptr = stringToNewUTF8(error);
            {{{ makeDynCall('vii', 'RtcApi.connectCompleteCallback') }}}(peerId, ptr);
            _free(ptr);
        } else {
            {{{ makeDynCall('vii', 'RtcApi.connectCompleteCallback') }}}(peerId, null);
        }
    },

    peers: [],
    next: 1,
    GetPeer: function (id) {
       return RtcApi.peers[id];
    },
    AddNextPeer: function (peer) {
       let id = RtcApi.next;
       RtcApi.next++;
       RtcApi.peers[id] = peer;
       return id;
    },
    RemovePeer: function (id) {
        RtcApi.peers[id] = undefined;
    },
}

function RtcInit(connectAnswerCallback, connectCompleteCallback) {
    console.log("RtcInit:", connectAnswerCallback, connectCompleteCallback);
    RtcApi.connectAnswerCallback = connectAnswerCallback;
    RtcApi.connectCompleteCallback = connectCompleteCallback;
}

function RtcConnect(offerPtr, receivedCallback) {
    var offerStr = UTF8ToString(offerPtr);
    let offer = JSON.parse(offerStr);
    console.log("RtcConnect: offer:", offer);

    //const STUN_URL = "stun:stun.sipsorcery.com";
    //pc = new RTCPeerConnection({ iceServers: [{ urls: STUN_URL }] });
    pc = new RTCPeerConnection();
    
    pc.onconnectionstatechange = (event) => {
        console.log("RtcConnect: onconnectionstatechange: " + pc.connectionState, event);
    }
    pc.onicegatheringstatechange = function () {
        console.log("RtcConnect: onicegatheringstatechange: " + pc.iceGatheringState);
    }
    pc.oniceconnectionstatechange = function () {
        console.log("RtcConnect: oniceconnectionstatechange: " + pc.iceConnectionState);
    }
    pc.onsignalingstatechange = function () {
        console.log("RtcConnect: onsignalingstatechange: " + pc.signalingState);
    }
    pc.onicecandidate = async function (event) {
        console.log('RtcConnect: onicecandidate: ', event.candidate);
        if (event.candidate) {
            //TODO: send candidates to server
            // await fetch(setIceCandidateUrl, {
            //     method: 'POST',
            //     body: JSON.stringify(event.candidate),
            //     headers: { 'Content-Type': 'application/json' }
            // });
        }
    };
    pc.onicecandidateerror = function (event) {
        console.log("RtcConnect: onicecandidateerror: ", event);
    }

    pc.ondatachannel = (event) => {
        const channel = event.channel
        console.log("RtcConnect: ondatachannel: ", channel);
        channel.onmessage = function (event) {
            console.log('RtcConnect: onmessage:', event.data);
            //TODO: receivedCallback
        }
    }

    const peerId = RtcApi.AddNextPeer(pc);

    pc.setRemoteDescription(offer).then(async () => {
        console.log("RtcConnect: creating answer");
        let answer = await pc.createAnswer();
        console.log("RtcConnect: assign and return answer: ", answer);
        await pc.setLocalDescription(answer);
        RtcApi.CallConnectAnswer(peerId, JSON.stringify(answer));
    }).catch((e) => {
        RtcApi.CallConnectComplete(peerId, e.message);
    });

    console.log("RtcConnect: peerId:", peerId);
    return peerId;
}

const RtcApiLib = {
    $RtcApi: RtcApi,
    RtcInit,
    RtcConnect,
};

autoAddDeps(RtcApiLib, "$RtcApi");
mergeInto(LibraryManager.library, RtcApiLib);
 