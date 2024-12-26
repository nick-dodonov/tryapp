mergeInto(LibraryManager.library, {
    Hello: function () {
        window.alert("Hello, world!");
    },
    SetupTestCallback: function (message, callback) {
        console.log("SetupTestCallback-1:", message);
        var stringMessage = UTF8ToString(message);
        console.log("SetupTestCallback-2:", stringMessage);
        let frameId = 0;
        //let timer = setTimeout(function () {
        let timer = setInterval(() => {
            console.log(">>>> SetupTestCallback-timer:", frameId);
            var buffer = stringToNewUTF8(stringMessage + "-" + frameId); // copy of message because it might be deleted before callback is run
            {{{ makeDynCall('vi', 'callback') }}} (buffer);
            _free(buffer);
            console.log("<<<< SetupTestCallback-timer:", frameId);

            ++frameId;
            if (frameId >= 5) {
                console.log("SetupTestCallback: cleanup");
                clearInterval(timer);
            }
        }, 1000);
    }    
});
