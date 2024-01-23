/**
 * Theme: Metrica - Responsive Bootstrap 4 Admin Dashboard
 * Author: Mannatthemes
 * Module/App: Main Js
 */


(function ($) {

    'use strict';

    function initSlimscroll() {
        $('.slimscroll').slimscroll({
            height: 'auto',
            position: 'right',
            size: "7px",
            color: '#e0e5f1',
            opacity: 1,
            wheelStep: 5,
            touchScrollStep: 50
        });
    }


    function initMetisMenu() {
        //metis menu
        $(".metismenu").metisMenu();
    }

    function initLeftMenuCollapse() {
        // Left menu collapse
        $('.button-menu-mobile').on('click', function (event) {
            event.preventDefault();
            $("body").toggleClass("enlarge-menu");
            initSlimscroll();
        });
    }

    function initEnlarge() {
        if ($(window).width() < 1025) {
            $('body').addClass('enlarge-menu');
        } else {
            if ($('body').data('keep-enlarged') != true)
                $('body').removeClass('enlarge-menu');
        }
    }

    function initTooltipPlugin() {
        $.fn.tooltip && $('[data-toggle="tooltip"]').tooltip()
    }



    function initActiveMenu() {
        // === following js will activate the menu in left side bar based on url ====
        $(".left-sidenav a").each(function () {
            var pageUrl = window.location.href.split(/[?#]/)[0];
            if (this.href == pageUrl) {
                $(this).addClass("active");
                $(this).parent().addClass("active"); // add active to li of the current link    
                $(this).parent().addClass("mm-active");
                $(this).parent().parent().addClass("in");
                $(this).parent().parent().addClass("mm-show");
                $(this).parent().parent().parent().addClass("mm-active");
                $(this).parent().parent().prev().addClass("active"); // add active class to an anchor
                $(this).parent().parent().parent().addClass("active");
                $(this).parent().parent().parent().parent().addClass("mm-show"); // add active to li of the current link                
                $(this).parent().parent().parent().parent().parent().addClass("mm-active");

            }
        });
    }



    function init() {
        initSlimscroll();
        initMetisMenu();
        initLeftMenuCollapse();
        initEnlarge();
        initTooltipPlugin();
        initActiveMenu();
        Waves.init();
        ////Confirm
        $(document)
            .off("click", "[data-type='confirm']")
            .on("click", "[data-type='confirm']", function (e) {
                e.preventDefault();
                var title = $(this).attr("title");
                var href = $(this).attr("href");
                if (confirm(title) == true) {
                    if (href) location.href = href;
                }
                return false;
            });
        if (!$(".wait_loading").length) {
            $(".preloader").fadeOut();

            if ($(".chosen").length) {
                $(".chosen").chosen({
                    search_contains: true
                });
            }
            if ($(".dropify").length) {
                $('.dropify').dropify();
            }
            if ($(".money").length) {
                $(".money").inputmask("numeric", {
                    radixPoint: ".",
                    groupSeparator: ",",
                    autoGroup: true,
                    suffix: ' VND', //No Space, this will truncate the first character
                    rightAlign: false,
                    oncleared: function () {
                        self.Value('');
                    }
                });
            }
        }

        //Swal.fire(
        //	'Thông báo bảo trì!',
        //	'Hệ thống sẽ bảo trì lúc 7h30 sáng ngày 15/11/2022 và dự kiến sẽ mở lại lúc 11h sáng ngày 15/11/2022.',
        //	'warning'
        //)
    }

    init();

})(jQuery)
function random_text() {

    $(".random_color").each(function () {
        var str = $(this).text();
        var hex = color.hex(str);
        $(this).css({ "background": hex });
    })
}

function fillForm(form, data) {
    $("input, select, textarea", form).not("[type=file]").each(function () {
        var type = $(this).attr("type");
        var name = $(this).attr("name");
        if (!name) return;
        name = name.replace("[]", "");
        var value = "";
        if ($(this).hasClass("input-tmp")) return;
        if ($.type(data[name]) !== "undefined" && $.type(data[name]) !== "null") {
            value = data[name];
        } else {
            return;
        }
        //console.log(value);
        if (name == "date_effect" || name == "date_expire" || name == "date_review") {
            value = moment(value).format("YYYY-MM-DD");
            //console.log(value);
        }
        switch (type) {
            case "checkbox":
                $(this).prop("checked", false);
                var rdvalue = $(this).val();
                //value = value.toString();
                //console.log(value);
                //console.log(typeof (rdvalue));
                //console.log(rdvalue == value);

                if (typeof (value) == "object" && value.indexOf(rdvalue) != -1) {
                    $(this).prop("checked", true);
                } else if (typeof (value) != "object" && rdvalue == value.toString()) {
                    $(this).prop("checked", true);
                }
                break;
            case "radio":
                $(this).removeAttr("checked", "checked");
                var rdvalue = $(this).val();
                if (rdvalue == value) {
                    $(this).prop("checked", true);
                }
                break;
            default:
                $(this).val(value);
                if ($(this).hasClass("chosen")) {
                    $(this).trigger("chosen:updated");
                }
                break;
        }
    });
};
