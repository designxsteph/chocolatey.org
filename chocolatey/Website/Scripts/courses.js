$(document).ready(function () {
    $('fieldset').parent().attr('id', 'quiz');
    $("#quiz .disable input").attr("disabled", "disabled");
    $("#quiz .mod-completed input[value='1']").addClass("correct").prop("checked", true).parent().children().attr("disabled", "disabled");
    $("#quiz .mod-completed .btn").addClass("d-none");
    // Completed Module Pop-up
    $('#quiz-modal').modal('show');

    // Quiz
    $(document).change(function () {
        var numItems = $('#quiz .form-field').length;
        var checkedItems = $("#quiz input:checked").length;

        if (checkedItems == numItems) {
            $("#quiz .btn").removeClass("disabled");
        }
    });
    $("form .btn").click(function (event) {
        var numItems = $('#quiz .form-field').length;
        var correctItems = $('#quiz .correct').length + $('#quiz .true').length;

        if (correctItems != numItems) {
            event.preventDefault();

            $(this).removeClass("btn-primary").addClass("btn-danger").attr("value", "Recheck Answsers");
            $("input").not("input:checked").removeClass("false");
            $(".true").removeClass("true").addClass("correct");
            $(".false").removeClass("false").addClass("wrong");
            $(".correct").parent().children().removeClass("wrong").not(".correct").attr("disabled", "disabled");
        }
    });
    $("input:radio").click(function (ev) {
        if (ev.currentTarget.value == "1") {
            $(this).addClass('true');

        } else if (ev.currentTarget.value == "0") {
            $(this).addClass('false');
            $(this).parent().children().removeClass("true");
        }
        $(this).parent().children().removeClass("wrong");
    });

    // Highlight Syntax
    $('pre').addClass('line-numbers py-2 m-0');
    Prism.highlightAll();
});