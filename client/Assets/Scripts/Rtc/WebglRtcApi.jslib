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
    
    receivedCallback: null,
    CallReceived: function(peerId, data) {
        //console.log("RtcApi: CallReceived:", peerId, typeof data, data);
        if (data) {
            if (data.constructor === String) {
                // const ptr = stringToNewUTF8(data);
                // const size = new TextEncoder().encode(data).length;
                // {{{ makeDynCall('vii', 'RtcApi.receivedCallback') }}}(peerId, ptr, size);
                // _free(ptr);
                // return;
                data = new TextEncoder().encode(data); //Uint8Array
            } else if (data instanceof ArrayBuffer) {
                data = new Uint8Array(data);
            } else {
                console.log("RtcApi: CallReceived: TODO: handle unsupported yet data type:", data);
                return;
            }
            
            const ptr = _malloc(data.byteLength);
            HEAPU8.set(data, ptr);
            {{{ makeDynCall('viii', 'RtcApi.receivedCallback') }}}(peerId, ptr, data.byteLength);
            _free(ptr);
        } else {
            {{{ makeDynCall('viii', 'RtcApi.receivedCallback') }}}(peerId, null, 0);
        }
    },

    peers: [],
    channels: [],
    next: 0,
    GetPeer: function (id) {
       return RtcApi.peers[id];
    },
    GetChannel: function (id) {
        return RtcApi.channels[id];
    },
    AddNextPeer: function (peer) {
       let id = RtcApi.next;
       RtcApi.next++;
       RtcApi.peers[id] = peer;
       return id;
    },
    SetChannel: function (id, channel) {
        RtcApi.channels[id] = channel;
    },
    RemovePeer: function (id) {
        RtcApi.channels[id] = undefined;
        RtcApi.peers[id] = undefined;
    },
}

function RtcInit(connectAnswerCallback, connectCompleteCallback, receivedCallback) {
    console.log("RtcInit:", 
        connectAnswerCallback, 
        connectCompleteCallback, 
        receivedCallback);
    RtcApi.connectAnswerCallback = connectAnswerCallback;
    RtcApi.connectCompleteCallback = connectCompleteCallback;
    RtcApi.receivedCallback = receivedCallback;
}

function RtcConnect(offerPtr) {
    var offerStr = UTF8ToString(offerPtr);
    let offer = JSON.parse(offerStr);
    console.log("RtcConnect: offer:", offer);

    //const STUN_URL = "stun:stun.sipsorcery.com";
    //pc = new RTCPeerConnection({ iceServers: [{ urls: STUN_URL }] });
    pc = new RTCPeerConnection();
    const peerId = RtcApi.AddNextPeer(pc);
    
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
        }
    };
    pc.onicecandidateerror = function (event) {
        console.log("RtcConnect: onicecandidateerror: ", event);
    }

    pc.ondatachannel = (event) => {
        const channel = event.channel
        console.log("RtcConnect: ondatachannel: ", channel);
        RtcApi.SetChannel(peerId, channel);
        RtcApi.CallConnectComplete(peerId, null);
        channel.onmessage = function (event) {
            //console.log('RtcConnect: onmessage:', event.data);
            RtcApi.CallReceived(peerId, event.data);
        }
    }

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

function RtcClose(peerId) {
    console.log("RtcClose: peerId:", peerId);
    const pc = RtcApi.GetPeer(peerId);
    pc.close();
    RtcApi.RemovePeer(peerId);
}

function RtcSend(peerId, bytes, size) {
    const channel = RtcApi.GetChannel(peerId);
    if (channel) {
        console.log("RtcSend:", peerId, bytes, size);
        const data = new Uint8Array(HEAPU8.buffer, bytes, size);
        channel.send(data);
    } else {
        console.log("RtcSend: ERROR: failed to find peer", peerId, bytes, size);
    }
}

const RtcApiLib = {
    $RtcApi: RtcApi,
    RtcInit,
    RtcConnect,
    RtcClose,
    RtcSend,
};

autoAddDeps(RtcApiLib, "$RtcApi");
mergeInto(LibraryManager.library, RtcApiLib);
 