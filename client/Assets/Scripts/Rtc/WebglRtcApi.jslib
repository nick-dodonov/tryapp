const RtcApi = {
    connectCallback: null,
    CallConnectCallback: function(peerId, error) {
        if (error) {
            const errorPtr = stringToNewUTF8(error);
            {{{ makeDynCall('vii', 'RtcApi.connectCallback') }}}(peerId, errorPtr);
            _free(errorPtr);
        } else {
            {{{ makeDynCall('vii', 'RtcApi.connectCallback') }}}(peerId, null);
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

function RtcInit(connectCallback) {
    console.log("RtcInit:", connectCallback);
    RtcApi.connectCallback = connectCallback;
}

function RtcConnect(offerPtr, receivedCallback) {
    console.log("RtcConnect: offer ptr:", offerPtr);
    var offerStr = UTF8ToString(offerPtr);
    console.log("RtcConnect: offer str:", offerStr);
    let offer = JSON.parse(offerStr);
    console.log("RtcConnect: offer:", offer);

    //const STUN_URL = "stun:stun.sipsorcery.com";
    //pc = new RTCPeerConnection({ iceServers: [{ urls: STUN_URL }] });
    pc = new RTCPeerConnection();
    
    pc.onconnectionstatechange = (event) => {
        console.log("RtcConnect: onconnectionstatechange: " + pc.connectionState, event);
    }

    const peerId = RtcApi.AddNextPeer(pc);
    RtcApi.CallConnectCallback(peerId, "connecting");

    pc.setRemoteDescription(offer).then(() => {
        RtcApi.CallConnectCallback(peerId, null); //"remote set SUCCESS");
        {{{ makeDynCall('vi', 'receivedCallback') }}}(null, 0);
    }).catch((e) => {
        RtcApi.CallConnectCallback(peerId, "remote set ERROR:" + e);
    });

    console.log("RtcConnect: result peerId:", peerId);
    return peerId;
}

const RtcApiLib = {
    $RtcApi: RtcApi,
    RtcInit,
    RtcConnect,
};

autoAddDeps(RtcApiLib, "$RtcApi");
mergeInto(LibraryManager.library, RtcApiLib);
 