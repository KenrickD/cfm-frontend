(function ($) {

    "use strict";

    // PRE LOADER
    $(window).on('load', function () {
        $('.preloader').fadeOut(1000); // set duration in brackets  

        if ($(".navbar").offset().top > 0) {
            $(".navbar-fixed-top").addClass("top-nav-collapse");
        } else {
            $(".navbar-fixed-top").addClass("top-nav-collapse");
        }
    });


    // MENU
    $('.navbar-collapse a').on('click', function () {
        $(".navbar-collapse").collapse('hide');
    });

    //$(window).load(function () {
    //    if ($(".navbar").offset().top > 0) {
    //        $(".navbar-fixed-top").addClass("top-nav-collapse");
    //    } else {
    //        $(".navbar-fixed-top").addClass("top-nav-collapse");
    //    }
    //});


    // ABOUT SLIDER
    $('.owl-carousel').owlCarousel({
        animateOut: 'fadeOut',
        items: 1,
        loop: true,
        autoplayHoverPause: false,
        autoplay: true,
        smartSpeed: 1000,
    });


    // SMOOTHSCROLL
    $(function () {
        $('.custom-navbar a').on('click', function (event) {
            var $anchor = $(this);
            $('html, body').stop().animate({
                scrollTop: $($anchor.attr('href')).offset().top - 49
            }, 1000);
            event.preventDefault();
        });
    });

})(jQuery);
