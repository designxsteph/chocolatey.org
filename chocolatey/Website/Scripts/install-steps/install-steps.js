$(function () {
    var
        deploymentMethodCookie = getCookie('deployment_method'),
        individualMethodTab = $('#individual-method-tab'),
        organizationMethodTab = $('#organization-method-tab'),
        builderScriptInput = $('.internalRepoUrlInput'),
        defaultUrl = "http://internal/odata/repo",
        deploymentMethodHash = window.location.hash.toString().toLowerCase(),
        deploymentMethodHashArray = [];

    $('[data-deployment-method]').each(function () {
        if (!deploymentMethodHashArray.includes($(this).attr('href'))) {
            deploymentMethodHashArray.push($(this).attr('href'));
        }
    });

    if (deploymentMethodHashArray.includes(deploymentMethodHash)) {
        var deploymentMethod = $('[href="' + deploymentMethodHash + '"]').attr('data-deployment-method');
        document.cookie = "deployment_method=" + deploymentMethod + "; path=/";

        $('[data-deployment-method="' + deploymentMethod + '"]').tab('show');

        if (deploymentMethodHash.includes("individual")) {
            individualMethodTab.tab('show');
        } else {
            organizationMethodTab.tab('show');
        }
    } else if (deploymentMethodCookie) {
        $('[data-deployment-method="' + deploymentMethodCookie + '"]').tab('show');

        if (deploymentMethodCookie == "Individual") {
            individualMethodTab.tab('show');
        } else {
            organizationMethodTab.tab('show');
        }
    }

    $('.copy-command .toolbar a').each(function () {
        var copyCommand = $(this).parentsUntil('.code-toolbar').parent().find('code').attr('class').split(" ");
        $(this).addClass('btn-copy').attr('data-clipboard-target', '.' + copyCommand[0]);
    });
    $("code:contains('INTERNAL REPO URL')").html(function (_, html) {
        return html.replace(/(INTERNAL REPO URL)/g, '<span class="internalRepoUrl">$1</span>');
    });
    builderScriptInput.keyup(function () {
        var value = $(this).val();

        $('.internalRepoUrl').text(value);
        builderScriptInput.val(value);
        if (value == 0) {
            $('.internalRepoUrl').text(defaultUrl);
            if (!$(".contains-internal-repo-url").children().hasClass('internal-repo-url-warning')) {
                $(".contains-internal-repo-url").prepend('<p class="internal-repo-url-warning callout callout-danger bg-light text-danger font-weight-bold small">You must enter your internal repository url above before proceeding.</p>');
            }
        }
        else {
            $('.internal-repo-url-warning').fadeOut("slow", function () {
                $(this).remove();
            });
        }
    }).keyup();

    $('[data-deployment-method]').click(function () {
        var deploymentMethod = $(this).attr('data-deployment-method');

        // Set preferred deployment method to use for showing integration method
        document.cookie = "deployment_method=" + deploymentMethod + "; path=/";

        // When deployment method is changed on package page, also change inside builder modal and vice versa
        $('[data-deployment-method="' + deploymentMethod + '"]').tab('show');

        if (deploymentMethod == "Individual") {
            individualMethodTab.tab('show');
        } else {
            organizationMethodTab.tab('show');
        }
    });
});