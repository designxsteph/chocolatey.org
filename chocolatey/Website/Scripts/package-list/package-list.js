// Package Preferences
$(function () {
    var preferenceGridView = $('#preferenceGridView');
    var gridView = getCookie('preferenceGridView');
    var preferenceModView = $('#preferenceModView');
    var modView = getCookie('preferenceModView');
    if (!document.location.search.length) {
        if (gridView || modView) {
            $('#search-filters form').submit();
        }
    }
    // Save Preferences
    $('.btn-preferences').click(function () {
        if (preferenceGridView.prop("checked") == true) {
            document.cookie = "preferenceGridView=true";
            $("#preferenceGridView option[value='true']").prop('selected', true);
        }
        else if (preferenceGridView.prop("checked") == false) {
            document.cookie = "preferenceGridView=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            $("#preferenceGridView option[value='false']").prop('selected', true);
        }
        if (preferenceModView.prop("checked") == true) {
            document.cookie = "preferenceModView=true";
            $("#preferenceModView option[value='true']").prop('selected', true);
            //$("#moderatorQueue option[value='true']").prop('selected', true);
        }
        else if (preferenceModView.prop("checked") == false) {
            document.cookie = "preferenceModView=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            $("#preferenceModView option[value='false']").prop('selected', true);
            //$("#moderatorQueue option[value='false']").prop('selected', true);
        }
        $('#search-filters form').submit();
    });
    // Package Filtering
    $('#search-filters .form-control').change(function () {
        $('#search-filters form').submit();
    });
    // Package warning callout
    $('#callout-package-warning a[data-toggle="collapse"]').click(function () {
        document.cookie = "chocolatey_hide_packages_warning=true";
    });
});

// Disclaimer Model
$(function () {
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