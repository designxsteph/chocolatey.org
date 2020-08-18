// Package Preferences
$(function () {
    var preferenceGridView = $('#preferenceGridView');
    var gridView = getCookie("preferenceGridView");
    var preferenceModView = $('#preferenceModView');
    var modView = getCookie("preferenceModView");

    // Legacy script- Delete in 30 days for users to get cookied instead of using localstorage
    if (window.localStorage.getItem('view')) {
        document.cookie = "preferenceModView=true";
        localStorage.removeItem("view");
        location.reload();
    }
    // End legacy script
    if (gridView) {
        preferenceGridView.prop("checked", true);
    }
    if (modView) {
        preferenceModView.prop("checked", true);
    }
    // Save Preferences
    $('.btn-preferences').click(function () {
        if (preferenceGridView.prop("checked") == true) {
            document.cookie = "preferenceGridView=true";
        }
        else if (preferenceGridView.prop("checked") == false) {
            document.cookie = "preferenceGridView=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        }
        if (preferenceModView.prop("checked") == true) {
            document.cookie = "preferenceModView=true";
        }
        else if (preferenceModView.prop("checked") == false) {
            document.cookie = "preferenceModView=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        }
        location.reload();
    });
    // Package warning callout
    $('#callout-package-warning a[data-toggle="collapse"]').click(function () {
        document.cookie = "chocolatey_hide_packages_warning=true";
    });
});

// Package Filtering
$(function () {
    $("#sortOrder,#prerelease,#moderatorQueue,#moderationStatus").change(function () {
        $(this).closest("form").submit();
    });
    Closeable.modal("chocolatey_hide_packages_disclaimer");
    if (!getCookie('chocolatey_hide_packages_disclaimer')) {
        $(".modal-closeable").css('display', 'block');
    }
});

// Documentation Search Results
(function () {
    var cx = '013536524443644524775:xv95wv156yw';
    var gcse = document.createElement('script');
    gcse.type = 'text/javascript';
    gcse.async = true;
    gcse.src = 'https://cse.google.com/cse.js?cx=' + cx;
    var s = document.getElementsByTagName('script')[0];
    s.parentNode.insertBefore(gcse, s);
})();