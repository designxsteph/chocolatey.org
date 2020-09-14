// Add anchors
anchors.options = { placement: 'left' };
anchors.add();

// PDF Download Links
var downloadPdfLink = location.pathname.substr(location.pathname.lastIndexOf('/') + 1);
if (downloadPdfLink == 'docs') {
    downloadPdfLink = 'home';
}
$('.btn-pdf-download').attr('href', 'https://files.chocolatey.org/docs/' + downloadPdfLink + '.pdf');

// Syntax and Tables
$('pre').addClass('line-numbers border py-2');
$('pre:not([class*="language-"])').addClass('language-none');
Prism.highlightAll();
$('.btn-hide').click(function () {
    $('.btn-hide, .text-danger').addClass('d-none');
    $('#uninstall-scripts').removeClass('d-none').addClass('d-block');
});
$(".table").addClass("table-bordered table-striped").wrap("<div class='table-responsive-sm'>");

// Right side navigation
var rightNav = $('#RightNav');
if (rightNav.length > 0) {
    $('#RightNavAppend').append('<p class="mt-3"><strong>Table of Contents:</strong></p>').append(rightNav.html());
    rightNav.remove();

    $('.docs-right a[href*="#"]').on('click', function (e) {
        e.preventDefault()
        $('html, body').animate(
            { scrollTop: $($(this).attr('href')).offset().top }, 1100
        )
    });
} else {
    $('.docs-right').remove();
    $('.docs-body').removeClass('col-xl-8').addClass('col-xl-10');
}

// Search
var cx = '013536524443644524775:xv95wv156yw';
var gcse = document.createElement('script');
gcse.type = 'text/javascript';
gcse.async = true;
gcse.src = 'https://cse.google.com/cse.js?cx=' + cx;
var s = document.getElementsByTagName('script')[0];
s.parentNode.insertBefore(gcse, s);