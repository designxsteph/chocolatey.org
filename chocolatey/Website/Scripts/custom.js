// Preloader
$(window).on('load', function () {
    $('.authentication-error').remove();
    $('#loader').fadeOut(500, function () {
        $(this).remove();
    });
});

// Show modal on tempdata "message"
$('#tempdata-message').modal('show');

// Cookie Notice
var cookieNoticeAlert = $('#cookieNoticeAlert'),
    cookieNoticeName = 'chocolatey_hide_cookies_notice',
    cookieNotice = getCookie(cookieNoticeName);

if (cookieNotice) {
    cookieNoticeAlert.remove();
} else {
    cookieNoticeAlert.removeClass('d-none');
}

cookieNoticeAlert.find('button').click(function () {
    console.log(getCookieExpirationNever());
    if (~location.hostname.indexOf('chocolatey.org')) {
        document.cookie = cookieNoticeName + '=true; ' + getCookieExpirationNever() + 'path=/; domain=chocolatey.org;';
    } else {
        document.cookie = cookieNoticeName + '=true;' + getCookieExpirationNever() + 'path=/;';
    }
});
function getCookieExpirationNever() {
    var d = new Date();
    // 100 years in milliseconds: 100 years * 365 days * 24 hours * 60 minutes * 60 seconds * 1000ms
    d.setTime(d.getTime() + (100 * 365 * 24 * 60 * 60 * 1000));
    return 'expires=' + d.toUTCString() + ';';
}

// Top Navigation
$(document).ready(function () {
    // Top notice alert
    var topNoticeAlert = $('#topNoticeAlert'),
        topNotice = window.sessionStorage.getItem('notice');

    if (topNotice) {
        topNoticeAlert.remove();
    } else {
        topNoticeAlert.removeClass('d-none');
    }

    topNoticeAlert.find('button').click(function () {
        sessionStorage.setItem('notice', 'true');
    });

    // Dropdowns on desktop
    $(".dropdown").on("click.bs.dropdown", function (e) {
        $target = $(e.target);
        // Stop dropdown from collapsing if clicked inside, otherwise collapse
        if (!$target.hasClass("dropdown-toggle")) {
            e.stopPropagation();
        }
    });
    // Fade in animation
    $('.dropdown').on('show.bs.dropdown', function () {
        var height = $('header').outerHeight();
        var top = -$(window).scrollTop() + height;
        var $dropdown = $(this).find('.dropdown-menu').first();
        $dropdown.css("top", top);
        $dropdown.stop(true, true).fadeIn();
    });
    // Fade out animation
    $('.dropdown').on('hide.bs.dropdown', function () {
        $(this).find('.dropdown-menu').first().stop(true, true).fadeOut();
    });
    // Close the dropdown when page is scrolled
    $(window).on("scroll", function () {
        if (window.innerWidth > 992) {
            closeDropdowns();
        }
    });
    // Close the dropdown when viewport is resized on desktop
    $(window).on("resize", function () {
        if (window.innerWidth > 768) {
            closeSidebar();
        }
        if (window.innerWidth > 992) {
            closeDropdowns();
            closeNav();
        }
    });
    // Close the dropdown on mobile devices
    $('.goback').click(function () {
        closeDropdowns();
    });

    // Add/Remove fixed positioning for mobile
    $('.collapsing-sidebar').add('#topNav')
        .on('shown.bs.collapse', function () {
            if (window.innerWidth < 768) {
                $("body").addClass("position-fixed");

                if (!$(this).hasClass('collapsing-sidebar')) {
                    $(this).parent().addClass("position-fixed").css("z-index", "999").css("top", "0");
                }
            }
        })
        .on('hidden.bs.collapse', function () {
            $("body").removeClass("position-fixed");

            if (!$(this).hasClass('collapsing-sidebar')) {
                $(this).parent().removeClass("position-fixed");
            }
        });

    // Closes Sub Nav
    function closeDropdowns() {
        $(".dropdown.show").find(".dropdown-toggle").dropdown('toggle');
    }
    // Closes Main Nav
    function closeNav() {
        $(".navbar-collapse.show").collapse('toggle');
    }
    // Closes Sidebar
    function closeSidebar() {
        $(".collapsing-sidebar.show").collapse('toggle');
    }
});

// Show/Hide right side announcement bar notification badge
var announcementCookie = $('#announcementCookie').val();
var announcementCount = $('#announcementCount').val();

if (!getCookie(announcementCookie)) {
    $(".announcement-notification-badge").text(announcementCount).removeClass('d-none');
}

$(".btn-announcement-notifications").click(function () {
    if (!getCookie(announcementCookie)) {
        document.cookie = announcementCookie + "=true; path=/;";
        $(".announcement-notification-badge").addClass('d-none');
    }
});

// Opens tabbed/collapse information based on hash
$(function () {
    var urlHash = document.location.toString();
    if (urlHash.match('#')) {
        var tabNav = $('[data-toggle="tab"][href="#' + urlHash.split('#')[1] + '"]');
        var parentTabNav = '#' + tabNav.parentsUntil('.tab-pane').parent().addClass('tab-nested');
        parentTabNav = $('#' + $('.tab-pane.tab-nested').prop('id') + '-tab');
        // Open Tabs
        parentTabNav.tab('show');
        tabNav.tab('show');

        // Toggle Collpase
        var collapseNav = $($('[data-toggle="collapse"][href="#' + urlHash.split('#')[1] + '"]').attr('href'));
        collapseNav.collapse('show');

        // Scroll Tabs
        if (parentTabNav.length) {
            $('html, body').scrollTop(parentTabNav.offset().top - 30);
        }
        else if (tabNav.length) {
            $('html, body').scrollTop(tabNav.offset().top - 30);
        }
        // Scroll Collapse
        if (collapseNav.length) {
            collapseNav.on('shown.bs.collapse', function () {
                if (/pricing/.test(window.location.href)) {
                    $('html, body').scrollTop($(this).offset().top - 120);
                } else if (!window.sessionStorage.getItem('prevent-scroll')) {
                    $('html, body').scrollTop($(this).offset().top - 60);
                    if ($(this).attr('id') == 'files') {
                        window.sessionStorage.setItem('prevent-scroll', 'files');
                    }
                }
            });
        }
        if (collapseNav.length && collapseNav.attr('id') != 'files' && window.sessionStorage.getItem('prevent-scroll')) {
            sessionStorage.removeItem('prevent-scroll');
        }
    }
    // Change hash on tab/collapse click and prevent scrolling
    $('[data-toggle="tab"], [data-toggle="collapse"]').not('.d-hash-none').click(function (e) {
        if (history.pushState) {
            history.pushState(null, null, $(this).attr('href'));
        } else {
            window.location.hash = $(this).attr('href'); //Polyfill for old browsers
        }
    });
});

//Makes :contains case insensitive
$.expr[":"].contains = $.expr.createPseudo(function (arg) {
    return function (elem) {
        return $(elem).text().toUpperCase().indexOf(arg.toUpperCase()) >= 0;
    };
});

//Tooltip
$(function () {
    $('[data-toggle="tooltip"]').tooltip()
})
$('.tt').tooltip({
    trigger: 'hover',
    placement: 'top'
});
function setTooltip(btn, message) {
    btn.tooltip('hide')
        .attr('data-original-title', message)
        .tooltip('show');
}
function hideTooltip(btn, message) {
    setTimeout(function () {
        btn.tooltip('hide')
        .attr('data-original-title', message)
    }, 1000);
}

// Initialize clipboard and change text
var clipboard = new ClipboardJS('.tt');

clipboard.on('success', function (e) {
    var btn = $(e.trigger);
    setTooltip(btn, 'Copied');
    hideTooltip(btn, 'Copy');
});

// Make input text selectable with one click
$(document).on('click', 'input[type=text]', function () {
    this.select();
});

// Toggle and scroll to collapse elements on click
$('.collapse-nav').click(function () {
    // Expand collapsed element if not already
    $(this).parent().parent().find(".active").removeClass("active");
    $(this).addClass('active');
    if (!$(this.hash).hasClass('show')) {
        $(this.hash).collapse('show');
    }

    // Find scroll position and scroll to it
    var collapseScrollHeight = $(this.hash).offset().top - $('[data-toggle="collapse"][href="' + $(this).attr('href') + '"]').outerHeight(true);
    if ($('.sticky-top:not(.vertical-nav)').length) {
        collapseScrollHeight = collapseScrollHeight - $('.sticky-top').outerHeight(true);
    }
    $('html, body').animate({ scrollTop: collapseScrollHeight }, 1100 );
});

// Smooth Scroll
// Select all links with hashes
$('a[href*="#"]')
    // Remove links that don't actually link to anything
    .not('[href="#"]')
    .not('[href="#0"]')
    .not('[data-toggle="collapse"]')
    .not('[data-toggle="tab"]')
    .not('[data-toggle="pill"]')
    .not('[data-slide="prev"]')
    .not('[data-slide="next"]')
    .not('.collapse-nav')
    .click(function (event) {
        // Highlight active link if vertical nav
        var stickyNav = /pricing/.test(window.location.href);
        if (stickyNav) {
            $(".sticky-nav").find(".active").removeClass("active");
            $(this).addClass('active');
        }
        // On-page links
        if (
            location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '')
            &&
            location.hostname == this.hostname
        ) {
            // Figure out element to scroll to
            var target = $(this.hash);
            var top = $('.sticky-top').outerHeight();
            target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
            // Does a scroll target exist?
            if (target.length) {
                $('html, body').animate({
                    scrollTop: target.offset().top
                }, 1100, function () {
                    // Callback after animation
                    // Must change focus!
                    var $target = $(target);
                    $target.focus();
                    if ($target.is(":focus")) { // Checking if the target was focused
                        return false;
                    } else {
                        $target.attr('tabindex', '-1'); // Adding tabindex for elements not focusable
                        $target.focus(); // Set focus again
                    };
                });
        }
    }
});

// Right vertical navigation active highlight on scroll
$(function () {
    $(document).on("scroll", onScroll);
});
function onScroll(event) {
    var scrollPos = $(document).scrollTop();
    $('.docs-right a[href*="#"]').each(function () {
        var currLink = $(this);
        var refElement = $(currLink.attr("href"));
        var courses = /courses/.test(window.location.href);
        var top = $('.module-top').outerHeight();

        if (courses) {
            if (refElement.position().top <= scrollPos - top) {
            //if (refElement.position().top <= scrollPos) {
                $('.docs-right ul li').removeClass("active");
                currLink.parent().addClass("active");
            }
            else {
                currLink.parent().removeClass("active");
            }
        }
        else {
            if (refElement.position().top <= scrollPos) {
                $('.docs-right ul li').removeClass("active");
                currLink.parent().addClass("active");
            }
            else {
                currLink.parent().removeClass("active");
            }
        }
    });
}

// Copy Button for use throughout the website
var clipboard = new ClipboardJS('.btn-copy');
$('.btn-copy').click(function () {
    var $this = $(this);
    $this.html('<span class="fas fa-check text-white"></span> Command Text Coppied').removeClass('btn-secondary').addClass('btn-success');
    setTimeout(function () {
        $this.html('<span class="fas fa-clipboard"></span> Copy Command Text').removeClass('btn-success').addClass('btn-secondary');
    }, 2000);
});

// Allow Callouts to be dismissible
$('[class*="callout-"] .close').click(function () {
    $(this).closest('[class*="callout-"]').hide();
});

// Documentation & Styleguide left side navigation
$(function () {
    setNavigation();
});
function setNavigation() {
    var path = window.location.pathname;
    path = path.replace(/\/$/, "");
    path = decodeURIComponent(path);

    $(".docs-left a").each(function () {
        var href = $(this).attr('href');
        if (path.substring(0, href.indexOf('docs/').length) === href || path.substring(0, href.indexOf('styleguide/').length) === href) {
            $(this).closest('li').addClass('active').parent().parent().collapse('show').parent().parent().parent().collapse('show');
        }
    });
    // Courses Section - Set Localstorage Items
    // Active
    $(".course-list li a").each(function () {
        var href = $(this).attr('href');
        if (path.substring(0, href.indexOf('courses/').length) === href) {
            window.localStorage.setItem('active', href);
        }
    });
    // Set Completed courses if user is NOT logged in
    $(".course-list:not(.authenticated) li a").each(function () {
        var href = $(this).attr('href');
        if (path.substring(0, href.indexOf('courses/').length) === href) {
            var completed = localStorage.completed === undefined ? new Array() : JSON.parse(localStorage.completed);
            if ($.inArray(href, completed) == -1) //check that the element is not in the array
                completed.push(href);
            localStorage.completed = JSON.stringify(completed);
        }
    });
}

// Get Localstorage Items for Courses Section
$(function () {
    // Get Active Localstorage Item
    var active = window.localStorage.getItem('active');
    if (active) {
        $('.course-list li a[href="' + active + '"]').parent().addClass('active');
    }
    // Get Completed Localstorage Items
    var completed = localStorage.completed === undefined ? new Array() : JSON.parse(localStorage.completed); //get all completed items
    for (var i in completed) { //<-- completed is the name of the cookie
        if (!$('.course-list li a[href="' + completed[i] + '"]').parent().hasClass('active') && !$('.course-list').hasClass("authenticated")) // check if this is not active
        {
            $('.course-list li a[href="' + completed[i] + '"]').parent().addClass('completed');
        }
    }
    // Remove completed local storage if use is logged in, tracking progress through profile
    if ($(".course-list").hasClass("authenticated")) {
        localStorage.removeItem('completed')
    }
    // Styleize
    $(".course-list li").mouseover(function () {
        $(this).children().addClass("hover");
    });
    $(".course-list li").mouseleave(function () {
        $(this).children().removeClass("hover");
    });
});

// Removes text from links in additional-course section
$("#additional-courses .course-list a").each(function () {
    $(this).empty().append("<span class='additional-module'>...</span>");
});

// Delete extra space from code blocks
$("code").each(function () {
    $(this).html($.trim($(this).text()));
});

// Allow touch swiping of carousels on mobile devices
$(".carousel").on("touchstart", function (event) {
    var xClick = event.originalEvent.touches[0].pageX;
    $(this).one("touchmove", function (event) {
        var xMove = event.originalEvent.touches[0].pageX;
        if (Math.floor(xClick - xMove) > 5) {
            $(this).carousel('next');
        }
        else if (Math.floor(xClick - xMove) < -5) {
            $(this).carousel('prev');
        }
    });
    $(".carousel").on("touchend", function () {
        $(this).off("touchmove");
    });
});

// Stops video from playing when modal is closed or carousel is transitioned
$('.information-carousel')
    .on('shown.bs.modal', function () {
        $(this).carousel('pause');
    })
    .on('hide.bs.modal', function () {
        $(this).carousel('cycle');
    })
    .on('slide.bs.carousel', function () {
        $(this).find(".video-story .modal").modal('hide');
    });
$(window).on("scroll", function () {
    if (window.innerWidth > 1200) {
        $(".video-story .modal").modal('hide');
    }
});
$(".video-story .modal").on('show.bs.modal', function (e) {
    var iFrame = $(this).find("iframe");
    iFrame.attr("src", iFrame.attr("data-src"));
});
$(".video-story .modal").on('hide.bs.modal', function (e) {
    $(this).find("iframe").attr("src", "");
});

// Shuffles divs on load
$('.shuffle').each(function () {
    var divs = $(this).children().has('img');
    while (divs.length) {
        $(this).prepend(divs.splice(Math.floor(Math.random() * divs.length), 1)[0]);
    }
});

// Responsive Tabs
$(function () {
    var navTabs = $('.nav-tabs:not(.nav-tabs-install)');

    tabs();

    $(window).on("resize", function () {
        tabs();
    });

    function tabs() {
        if (window.innerWidth < 576) {
            navTabs.find('.nav-item').addClass('w-100');
            navTabs.find('.nav-link').addClass('btn btn-outline-primary').removeClass('nav-link');
        }
        else {
            navTabs.find('.nav-item').removeClass('w-100');
            navTabs.find('.btn').addClass('nav-link').removeClass('btn btn-outline-primary');
        }
    }
});

// Get cookies
function getCookie(name) {
    var pattern = RegExp(name + "=.[^;]*");
    var matched = document.cookie.match(pattern);
    if (matched) {
        var cookie = matched[0].split('=');
        return cookie[1];
    }
    return false;
}

// Set Login/Logoff Navigation
$(function () {
    // Only check authentication on certain parts of the site
    var authenticatedURL = window.location.href.indexOf("/packages") > -1 || window.location.href.indexOf("/courses") > -1 || window.location.href.indexOf("/account") > -1 || window.location.href.indexOf("/profiles") > -1;
    if (authenticatedURL) {
        $.ajax({
            type: "POST",
            url: window.location.protocol + "//" + window.location.host,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: authenticationSuccess
        });
    }
});

function authenticationSuccess(data, status) {
    var uxLogoff = $('.ux_logoff');
    var uxLogin = $('.ux_login');
    var uxProfile = $('.ux_profile');
    if (data.isAuthenticated) {
        uxLogoff.removeClass('d-none');
        uxLogin.addClass('d-none');
        uxProfile.find('a').prop('href', '/profiles/' + data.userName);
    } else {
        uxLogoff.addClass('d-none');
        uxLogin.removeClass('d-none');
    }
}

// Invisible input used for newsletter form
var tmpElement = document.createElement('input');
tmpElement.className = 'invisible-input';
tmpElement.setAttribute('aria-label', 'Invisible Input');
try {
    document.body.appendChild(tmpElement);
} catch (error) {
    // ignore
}

// Typewriter animation
if ($('.terminal-body').length) {
    var phrasesSpan = $('.terminal-body span[data-animate]');
    var phrases = $('.terminal-body span[data-animate]').attr('data-animate').split(',');
    var index = 0;
    var position = 0;
    var currentString = '';
    var direction = 1;
    var animate = function () {
        position += direction;
        if (!phrases[index]) {
            index = 0;
        } else if (position < -1) {
            index++;
            direction = 1;
        } else if (phrases[index][position] !== undefined) {
            currentString = phrases[index].substr(0, position);
            phrasesSpan = phrasesSpan.html(currentString);
            // if we've arrived at the last position reverse the direction
        } else if (position > 0 && !phrases[index][position]) {
            currentString = phrases[index].substr(0, position);
            direction = -1;
            phrasesSpan = phrasesSpan.html(currentString);
            return setTimeout(animate, 2000);
        }
        phrasesSpan = phrasesSpan.html(currentString);
        setTimeout(animate, 100);
    }
    animate();
}

// Lazy Load Images
$(function () {
    $(".lazy + noscript").remove();
});
document.addEventListener("DOMContentLoaded", function () {
    $.fn.isInViewport = function () {
        var elementTop = $(this).offset().top;
        var elementBottom = elementTop + $(this).outerHeight();

        var viewportTop = $(window).scrollTop();
        var viewportBottom = viewportTop + $(window).height();

        return elementBottom > viewportTop && elementTop < viewportBottom;
    };

    var lazyImages = [].slice.call(document.querySelectorAll("img.lazy"));
    var active = false;

    var lazyLoad = function () {
        if (active === false) {
            active = true;

            setTimeout(function () {
                lazyImages.forEach(function (lazyImage) {
                    if ((lazyImage.getBoundingClientRect().top <= window.innerHeight && lazyImage.getBoundingClientRect().bottom >= 0) && getComputedStyle(lazyImage).display !== "none") {
                        lazyImage.src = lazyImage.dataset.src;
                        lazyImage.classList.remove("lazy");

                        lazyImages = lazyImages.filter(function (image) {
                            return image !== lazyImage;
                        });

                        if (lazyImages.length === 0) {
                            document.removeEventListener("scroll", lazyLoad);
                            window.removeEventListener("resize", lazyLoad);
                            window.removeEventListener("orientationchange", lazyLoad);
                        }
                    }
                });

                active = false;
            }, 200);
        }
    };

    document.addEventListener("scroll", lazyLoad);
    window.addEventListener("resize", lazyLoad);
    window.addEventListener("orientationchange", lazyLoad);
    $('.lazy').each(function () {
        if ($(this).isInViewport() && $(this).parent().parent().parent().hasClass("carousel-item")) {
            $('.carousel').on('slide.bs.carousel', function () {
                lazyLoad();
            });
        }
        else if ($(this).isInViewport() && !$(this).parent().parent().parent().hasClass("carousel-item")) {
            $(this).attr("src", $(this).attr("data-src"));
        }
    });
});

// Replace Show/Hide on buttons when clicked
$('.btn').click(function () {
    var $this = $(this);
    if ($this.is(':contains("Show")')) {
        $this.each(function () {
            var text = $this.text().replace('Show', 'Hide');
            $this.text(text);
        });
    } else if ($this.is(':contains("Hide")')) {
        $this.each(function () {
            var text = $this.text().replace('Hide', 'Show');
            $this.text(text);
        });
    }
});

// Search box
$('.nav-search .btn-search').click(function () {
    var btnSearch = $('.nav-search .btn-search');

    btnSearch.addClass('d-none').parent().find('.nav-search-container').removeClass('d-none').find('form').removeClass('d-none').find('input').focus();
    if (window.innerWidth < 625) {
        $('#topNav').find('.navbar-brand').addClass('d-none').next().addClass('w-100').find('.nav-search').addClass('w-100');
        $('#topNav').find('.btn-nav-toggle').parent().addClass('d-none');
        $('#topNav').find('.btn-announcements').parent().addClass('d-none');
    }
    searchHelpShow();
});
$(window).on("resize click", function () {
    if ($('.nav-search .btn-search').hasClass('d-none')) {
        $('.nav-search-container').addClass('d-none').find('form').addClass('d-none');
        $('.btn-search').removeClass('d-none');

        if (window.innerWidth < 625) {
            $('#topNav').find('.navbar-brand').removeClass('d-none').next().removeClass('w-100').find('.nav-search').removeClass('w-100');
            $('#topNav').find('.btn-nav-toggle').parent().removeClass('d-none');
            $('#topNav').find('.btn-announcements').parent().removeClass('d-none');
        }
    }
    searchHelpHide();
});
$('.search-box input').bind("click keyup", function () {
    if (!$(this).hasClass('active-input')) {
        $(this).addClass('active-input');
        searchHelpShow();
    }
});
function searchHelpShow() {
    if ($('.nav-search .btn-search').hasClass('d-none')) {
        $('.nav-search').find('.search-box input').addClass('active-input');
    }
    $('.active-input').parentsUntil('form').parent().find('.search-help').removeClass('d-none');
}
function searchHelpHide() {
    $('.active-input').removeClass('active-input').parentsUntil('form').parent().find('.search-help').addClass('d-none');
}
$('.nav-search button, .search-box input, .search-box button, .search-box .search-help').click(function (event) {
    event.stopPropagation();
});

// Show image overlays on <video> element until clicked
$.each($('.video-overlay'), function () {
    $($(this)).click(function () {
        var videoOverlayImage = $(this).find('.video-overlay-image');
        var videoOverlayEmbed = $(this).find('.video-overlay-embed');

        if (videoOverlayEmbed.hasClass('d-none')) {
            videoOverlayImage.addClass('d-none');
            videoOverlayEmbed.removeClass('d-none');
            videoOverlayEmbed.get(0).play();
        }
    });
});

// Style blockquotes in markdown based on content
$.each($('blockquote'), function () {
    var warningEmoji = String.fromCodePoint(0x26A0);

    if ($(this).text().indexOf(warningEmoji) >= 0) {
        // Contains warning emoji
        $(this).addClass('callout-warning');
    }
});

// Countdown clocks
$(function () {
    var countdownDateTime = $('.countdown-date-time'),
        countdownContainer = $('.countdown-container'),
        ellapsedButtonText = 'Watch On-Demand Now',
        countdownContainerTime = '<div><div>%D</div><p>Days</p></div><div><div>%H</div><p>Hours</p></div><div><div>%M</div><p>Minutes</p></div><div><div>%S</div><p>Seconds</p></div>';

    if (countdownDateTime.length) {
        setCountdownTimer();
    }

    function setCountdownTimer() {
        $.each(countdownDateTime, function (i, val) {

            var upcomingEventTime = $(this).val();
            var eventListingContainer = $(this).parent();
            var eventListingContainerOnDemand = eventListingContainer.find('.btn-on-demand');

            if (getUTCNow($(this).val()) > getUTCNow(new Date())) {
                countdownContainer.each(function () {
                    $(this).countdown(upcomingEventTime, function (event) {
                        if (event.elapsed) {
                            // Go back and check for more times
                            setCountdownTimer();
                        } else {
                            // Show time
                            $(this).html(event.strftime(countdownContainerTime));
                        };
                    })
                });

                return false;
            } else {
                // Individual event complete
                eventListingContainer.find('.calendar-date').css('opacity', '.5');
                eventListingContainer.find('.btn:not(".btn-on-demand"):not(".btn-replay")').addClass('disabled');
                if (eventListingContainerOnDemand.length > 0) {
                    eventListingContainerOnDemand.html(eventListingContainerOnDemand.html().replace(eventListingContainerOnDemand.html(), ellapsedButtonText))
                }
                // If all times are past (event over)
                if (i == countdownDateTime.length - 1) {
                    $('#countdown-header.countdown-multi-event section').removeClass('pb-5').addClass('pb-3 pb-lg-5');
                    $('#countdown-header.countdown-single-event section').removeClass('pb-5').addClass('pb-0');
                    replaceElapsed();
                }
            }
        });
    }

    function replaceElapsed() {
        $('.countdown-details').add(countdownContainer).add($('.countdown-date')).add($('.btn-not-on-demand')).remove();
        $('a, h3').each(function () {
            $(this).html($(this).html()
                .replace('Reserve My Spot Now', ellapsedButtonText)
                .replace('Register Now', ellapsedButtonText)
                .replace('Register', ellapsedButtonText)
                .replace('Join Us Live On', 'Watch The Replays On'));
        });
    }

    function getUTCNow(c) {
        var now = new Date(c);
        var time = now.getTime();
        var offset = now.getTimezoneOffset();
        offset = offset * 60000;
        return time - offset;
    }

    // Uncomment below to pause timer to allow for styling
    //$('.countdown-container').countdown('pause');
});