// Preloader
$(window).on('load', function () {
    $('#loader').fadeOut(500, function () {
        $(this).remove();
    });
});

// Show modal on tempdata "message"
$('#tempdata-message').modal('show');

// Cookies Notice
var CookiesNotice = (function () {
    return {
        modal: function (cookieName) {
            $(".cookies-popup .cookies-close").click(function (e) {
                $(e.target).closest(".cookies-popup").hide();
                var d = new Date();
                // 100 years in milliseconds: 100 years * 365 days * 24 hours * 60 minutes * 60 seconds * 1000ms
                d.setTime(d.getTime() + (100 * 365 * 24 * 60 * 60 * 1000));
                var expires = "expires=" + d.toUTCString();
                document.cookie = cookieName + "=true;" + expires + ";path=/";
            });
        }
    }
})();
CookiesNotice.modal("chocolatey_hide_cookies_notice");
if (!getCookie('chocolatey_hide_cookies_notice')) {
    $(".cookies-popup").css('display', 'block');
}

// Top Navigation
$(document).ready(function () {
    // Top Alert
    var notice = window.sessionStorage.getItem('notice');
    if (notice) {
        $(".notice-text").addClass("d-none");
    }
    $('.notice-text').click(function () {
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
        if ($(window).width() < 992) {
            var height = $('header .alert').outerHeight();
        }
        else {
            var height = $('nav.navbar').outerHeight() + $('header .alert').outerHeight();
        }
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
        if ($(this).width() > 992) {
            closeDropdowns();
        }
    });
    // Close the dropdown when viewport is resized on desktop
    $(window).on("resize", function () {
        if ($(this).width() > 992) {
            closeDropdowns();
            closeNav();
        }
    });
    // Close the dropdown on mobile devices
    $('.goback').click(function () {
        closeDropdowns();
    });
    // Add/Remove fixed positioning for mobile
    $('#topNav').on('show.bs.collapse', function () {
        if ($(window).width() < 768) {
            $(this).parent().addClass("position-fixed").css("z-index", "999").css("top", "0");
            $("body").addClass("position-fixed");
        }
    });
    $('#topNav').on('hide.bs.collapse', function () {
        $(this).parent().removeClass("position-fixed");
        $("body").removeClass("position-fixed");
    });
    // Closes Sub Nav
    function closeDropdowns() {
        $(".dropdown.show").find(".dropdown-toggle").dropdown('toggle');
    }
    // Closes Main Nav
    function closeNav() {
        $(".navbar-collapse.show").collapse('toggle');
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
                } else {
                    $('html, body').scrollTop($(this).offset().top - 60);
                }
            });
        }
    }
    // Change hash on tab/collapse click and prevent scrolling
    $('[data-toggle="tab"], [data-toggle="collapse"]').not('.d-hash-none').click(function (e) {
        if (history.pushState) {
            history.pushState(null, null, e.target.hash);
        } else {
            window.location.hash = e.target.hash; //Polyfill for old browsers
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
    $(this).parent().parent().find(".active").removeClass("active");
    $(this).addClass('active');
    if (!$(this.hash).hasClass('show')) {
        $(this.hash).collapse('show');
    }
    $('html, body').animate({ scrollTop: $(this.hash).offset().top - 120 }, 1100);
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
$(function () {
    var pre = document.getElementsByTagName("code");
    for (var i = 0, len = pre.length; i < len; i++) {
        var text = pre[i].firstChild.nodeValue;
        if (text != null) {
            pre[i].firstChild.nodeValue = text.replace(/^\n+|\n+$/g, "");
        }
    }
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
    if ($(this).width() > 1200) {
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
    tabs();

    $(window).on("resize", function () {
        tabs();
    });

    function tabs() {
        if ($(window).width() < 576) {
            $(".nav-tabs .nav-item").addClass("w-100");
            $(".nav-tabs .nav-link").addClass("btn btn-outline-primary").removeClass("nav-link");
        }
        else {
            $(".nav-tabs .nav-item").removeClass("w-100");
            $(".nav-tabs .btn").addClass("nav-link").removeClass("btn btn-outline-primary");
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

// Set Login Cookie
$(function () {
    var acount = $(".ux_account");
    var action = $(".ux_account_action");
    if (getCookie('.ChocolateyGalleryAuthentication')) {
        acount.html('<span class="fas fa-cogs" alt="Account"></span><span>Account</span>');
        acount.prop('href', '/account');
        action.html('<span class="fas fa-sign-out-alt" alt="Log Off"></span><span>Log Off</span>');
        action.prop('href', '/users/account/LogOff');
    } else {
        acount.html('<span class="fas fa-sign-in-alt" alt="Login"></span><span>Login</span>');
        acount.prop('href', '/users/account/LogOn');
        action.html('<span class="fas fa-user-plus" alt="Signup"></span><span>Signup</span>');
        action.prop('href', '/account/Register');
    }
});

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
$('.search-box').each(function () {
    if (!$(this).parent().hasClass('nav-search')) {
        $(this).removeClass('d-none');
    }
});
$('.nav-search .btn-search').click(function () {
    var btnSearch = $('.nav-search .btn-search');
    var btnSearchOption = btnSearch.prev().find('button');

    btnSearch.addClass('d-none').parent().find('form').removeClass('d-none').find('input').focus();
    if (!btnSearchOption.hasClass('btn-docs') && document.location.pathname.indexOf("/docs") != 0 && document.location.pathname.indexOf("/resources") != 0) {
        btnSearchOption.html('<span class="small"><i class="fas fa-search" alt="Search Packages"></i> Packages</span>');
        btnSearchOption.after('<button class="btn btn-light btn-docs" type="submit" formaction="/docs/search"><span class="small"><i class="fas fa-file" alt="Search Docs"></i> Docs</span></button>');
    }
    navSearch();
    searchHelpShow();

    $(window).on("resize", function () {
        navSearch();
    });

    function navSearch() {
        if (btnSearch.hasClass('d-none')) {
            if ($(window).width() < 576) {
                $('#topNav').find('.navbar-brand').addClass('d-none').next().addClass('w-100').find('.nav-search').addClass('w-100');
                $('#topNav').find('.btn-nav-toggle').addClass('d-none');
            }
            else {
                $('#topNav').find('.navbar-brand').removeClass('d-none').next().removeClass('w-100').find('.nav-search').removeClass('w-100');
                $('#topNav').find('.btn-nav-toggle').removeClass('d-none');
            }
        }
    }
});
$(window).on("resize, click", function () {
    if ($('.nav-search .btn-search').hasClass('d-none')) {
        $('.nav-search form').addClass('d-none').next().removeClass('d-none');

        if ($(window).width() < 576) {
            $('#topNav').find('.navbar-brand').removeClass('d-none').next().removeClass('w-100').find('.nav-search').removeClass('w-100');
            $('#topNav').find('.btn-nav-toggle').removeClass('d-none');
        }
    }
    searchHelpHide();
});
$('.search-box.search-packages input').bind("click keyup", function () {
    if (!$(this).hasClass('active-input')) {
        $(this).addClass('active-input');
        searchHelpShow();
    }
});
function searchHelpShow() {
    if ($('.nav-search .btn-search').hasClass('d-none') && $('.nav-search .search-box').hasClass('search-packages')) {
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