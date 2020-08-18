var Closeable = (function () {

    return {
        modal: function(cookieName) {
            $(".modal-closeable .close").click(function (e) {
                $(e.target).closest(".modal-closeable").hide();
                var d = new Date();
                // 100 years in milliseconds: 100 years * 365 days * 24 hours * 60 minutes * 60 seconds * 1000ms
                d.setTime(d.getTime() + (100 * 365 * 24 * 60 * 60 * 1000));
                var expires = "expires=" + d.toUTCString();
                document.cookie = cookieName + "=true;" + expires + ";path=/";
            });
        }
    }

})();