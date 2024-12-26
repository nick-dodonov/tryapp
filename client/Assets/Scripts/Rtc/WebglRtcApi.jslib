function xxxx() {
    console.log("XXXXXXXXX");
}

mergeInto(LibraryManager.library, {
    Connect: async function() {
        const currentUrl = window.location.href;
        console.log("WebglRtcClient: currentUrl: ", currentUrl);
        const baseUrl = currentUrl.replace(/^(https?:\/\/[^\/]+)(\/.*)?$/, (match, base) => {
            if (base.includes("localhost")) {
                return base.replace(/:\d+/, ":5270") + "/api/";
            }
            return base + "/api/";
        });
        console.log("WebglRtcClient: baseUrl: ", baseUrl);
        const id = Date.now().toString();
        const getOfferUrl = `${baseUrl}getoffer?id=${id}`;
        const setAnswerUrl = `${baseUrl}setanswer?id=${id}`;

        console.log("Connect:", getOfferUrl);
        let offerResponse = await fetch(getOfferUrl);
        let offer = await offerResponse.json();
        console.log("result offer: ", offer);
    },
    SetupTestCallback: function(message, callback) {
        console.log("SetupTestCallback-1:", message);
        var stringMessage = UTF8ToString(message);
        console.log("SetupTestCallback-2:", stringMessage);
        let frameId = 0;
        //let timer = setTimeout(function () {
        let timer = setInterval(() => {
            console.log(">>>> SetupTestCallback-timer:", frameId);
            var buffer = stringToNewUTF8(stringMessage + "-" + frameId); // copy of message because it might be deleted before callback is run
            {
                {
                    {
                        makeDynCall('vi', 'callback')
                    }
                }
            }(buffer);
            _free(buffer);
            console.log("<<<< SetupTestCallback-timer:", frameId);

            ++frameId;
            if (frameId >= 5) {
                console.log("SetupTestCallback: cleanup");
                clearInterval(timer);
            }
        }, 1000);
    },
});