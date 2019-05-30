$(function () {
    var
        packages = localStorage.packageList === undefined ? new Array() : JSON.parse(localStorage.packageList),
        getStorage,
        storageTitle,
        storageVersion,
        storageImage,
        storageValue,
        storageButton,
        storageIdentity,
        current,
        packageUrl,
        packageImage,
        packageTitle,
        packageValue,
        packageVersion,
        packageButton,
        packageImagePath,
        packageIdentity,
        builderNextStep,
        builderPrevStep,
        internalRepoUrl;

    const
        builderStep1 = $('#builder-step-1-tab'),
        builderStep3 = $('#builder-step-3-tab'),
        builderStep4 = $('.builder-step-4-tab'),
        builderStep5 = $('.builder-step-5-tab'),
        builderIndividual = $('.builder-individual'),
        builderOrganization = $('.builder-organization'),
        builderModal = $('#build-script'),
        builderScriptInput = $('.internalRepoUrlInput'),
        builderStorage = $('.storage'),
        builderViewBtn = $('.btn-view-builder'),
        builderNextBtn = $('.btn-next-step'),
        builderPrevBtn = $('.btn-prev-step');


    // Find Url on Display Image & Add Class
    // TODO: See about moving this logic to DisplayPackage.cshtml instead
    if ($('.package-logo').hasClass('package-image')) {
        if ($('.btn-builder').attr('value').indexOf("--") >= 0) {
            current = $('.btn-builder').attr('value').substr(0, $('.btn-builder').attr('value').indexOf('--')).trim();
        }
        else {
            current = $('.btn-builder').attr('value');
        }
        if (window.location.href.indexOf(current) > -1) {
            $('.package-image').addClass($('.btn-builder').attr('title').replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, ''));
        }
    }

    if (packages.length != 0) {
        builderViewBtn.removeClass('d-none');

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageTitle = getStorage[0];
            storageVersion = getStorage[1];
            storageImage = getStorage[2];
            storageValue = getStorage[3];
            storageButton = $('.btn-builder[value="' + storageValue + '"]');
            storageIdentity = storageTitle.replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, '');

            // Url
            if (storageValue.indexOf("--") >= 0) {
                packageUrl = storageValue.substr(0, storageValue.indexOf('--')).trim();
            }
            else {
                packageUrl = storageValue;
            }

            // Image Path
            if (storageImage == "packageDefaultIcon-50x50.png") {
                packageImagePath = "/Content/Images/";
            } else {
                packageImagePath = "/content/packageimages/";
            }

            if ((storageButton).length > 0) {
                packageIdentity = storageButton.attr('title').replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, '');

                // Update main list buttons
                storageButton.each(function () {
                    if ($(this).hasClass('btn-builder-text')) {
                        $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span> Remove from Script Builder');
                    } else {
                        $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span>');
                    }
                });
                storageButton.removeClass('btn-success').addClass('btn-danger').addClass(packageIdentity);
            }

            // Generate Package List
            builderStorage.append('<hr /><div id="' + storageIdentity + '" class="d-flex flex-row align-items-start storage-row ' + storageIdentity + '"><img class="package-image mr-2" src="' + packageImagePath + '' + storageImage + '" height="30" width="30"><div class="mr-2"><a class="text-dark btn-link mb-0 h5 text-break" href="/packages/' + packageUrl + '/' + storageVersion + '">' + storageTitle + '</a><p class="mb-0"><small>' + storageVersion + '</small></p></div><button class="btn btn-sm btn-builder-remove btn-danger ml-auto" value="' + storageValue + '" title="' + storageTitle + '" version="' + storageVersion + '" image="' + storageImage + '"><span class="fas fa-minus-circle"></span></button></div');
        }

        // Count items
        countPackages();
    }

    // Button click inside of builder list
    removePackages();


    $('.btn-builder').each(function () {
        $(this).click(function () {
            packageTitle = $(this).attr('title'),
            packageValue = $(this).attr('value'),
            packageIdentity = packageTitle.replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, ''),
            packageVersion = $(this).attr('version'),
            packageButton = $('.btn-builder[value="' + packageValue + '"]');

            // Find Url
            if (packageValue.indexOf("--") >= 0) {
                packageUrl = packageValue.substr(0, packageValue.indexOf('--')).trim();
            }
            else {
                packageUrl = packageValue;
            }

            // Find Image
            if (window.location.href.indexOf(packageUrl) > -1) {
                packageImage = /[^/]*$/.exec($(this).parentsUntil('body').parent().find('.package-image.' + packageIdentity).attr('src'))[0];
            } else {
                packageImage = /[^/]*$/.exec($(this).parentsUntil('.package-item').parent().find(".package-image").attr('src'))[0];
            }

            if (packageImage.indexOf("packageDefaultIcon") >= 0) {
                packageImage = "packageDefaultIcon-50x50.png";
            }

            // Determine if there is already a version of the package in their list
            for (var i in packages) {
                if (packages.length != 0 && $(this).hasClass('btn-success')) {
                    getStorage = packages[i].split(" , ");
                    storageTitle = getStorage[0];
                    storageVersion = getStorage[1];
                    storageImage = getStorage[2];
                    storageValue = getStorage[3];
                    if (storageTitle == packageTitle) {
                        $(this).addClass('active'); // Prevents from continueing on through the funtion and adding to builder
                        $('.btn-version').attr('title', packageTitle).attr('image', packageImage).attr('version', packageVersion).attr('value', packageValue);
                        $('.current-version').text(storageVersion);
                        $('.new-version').text(packageVersion);
                        $('#warning-version').modal('show');
                    }
                }
            }

            // If no value matching found, add or delete item
            // Delete items
            if ($(this).hasClass('btn-danger') && !$(this).hasClass('active')) {
                // Change button state
                packageButton.each(function () {
                    if ($(this).hasClass('btn-builder-text')) {
                        $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span> Add to Script Builder');
                    } else {
                        $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span>');
                    }
                });
                packageButton.removeClass('btn-danger').addClass('btn-success').removeClass(packageIdentity);
                
                // Remove package from list
                $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).prev().remove();
                $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).remove();

                // Delete & Update TitleVersion from Storage
                for (var i in packages) {
                    if (packages[i] == packageTitle + " , " + packageVersion + " , " + packageImage + " , " + packageValue) {
                        packages.splice(i, 1);
                    }
                }
                localStorage.packageList = JSON.stringify(packages);

                // Storage
                removeStorage();

                // Count items
                countPackages();
            }

            // Add items
            else if ($(this).hasClass('btn-success') && !$(this).hasClass('active')) {
                // Save Title & Version to Storage
                packages.push(packageTitle + " , " + packageVersion + " , " + packageImage + " , " + packageValue);
                localStorage.packageList = JSON.stringify(packages);

                // Change button state
                packageButton.each(function () {
                    if ($(this).hasClass('btn-builder-text')) {
                        $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span> Remove from Script Builder');
                    } else {
                        $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span>');
                    }
                });
                packageButton.removeClass('btn-success').addClass('btn-danger').addClass(packageIdentity);


                // Show builder tab
                builderViewBtn.removeClass('d-none');

                // Count items
                countPackages();

                // Find Image Path
                if (packageImage == "packageDefaultIcon-50x50.png") {
                    packageImagePath = "/Content/Images/";
                } else {
                    packageImagePath = "/content/packageimages/";
                }

                // Add package to list
                builderStorage.append('<div id="' + packageIdentity + '" class="d-flex flex-row align-items-start storage-row ' + packageIdentity + '"><img class="package-image mr-2" src="' + packageImagePath + '' + packageImage + '" height="30" width="30"><div class="mr-2"><a class="text-dark btn-link mb-0 h5 text-break" href="/packages/' + packageUrl + '/' + packageVersion + '">' + packageTitle + '</a><p class="mb-0"><small>' + packageVersion + '</small></p></div><button class="btn btn-sm btn-builder-remove btn-danger ml-auto" value="' + packageValue + '" title="' + packageTitle + '" version="' + packageVersion + '" image="' + packageImage + '"><span class="fas fa-minus-circle"></span></button></div');
                $('<hr />').insertBefore('.storage #' + packageIdentity + '');

                // Button click inside of builder list
                removePackages();
            }
        });
    });

    // Clicked to remove version and add new version
    $('.btn-version').click(function () {
        packageTitle = $(this).attr('title'),
        packageValue = $(this).attr('value'),
        packageIdentity = packageTitle.replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, ''),
        packageVersion = $(this).attr('version'),
        packageButton = $('.btn-builder[value="' + packageValue + '"]'),
        packageImage = $(this).attr('image');

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageTitle = getStorage[0];

            // Delete current version
            if (storageTitle == packageTitle) {
                packages.splice(i, 1);
            }
        }

        // Add new version to storage
        packages.push(packageTitle + " , " + packageVersion + " , " + packageImage + " , " + packageValue);
        localStorage.packageList = JSON.stringify(packages);

        // Change button state on main page
        $(".btn-builder." + packageIdentity).each(function () {
            if ($(this).hasClass('btn-builder-text')) {
                $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span> Add to Script Builder');
            } else {
                $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span>');
            }
        });
        $(".btn-builder." + packageIdentity).removeClass('btn-danger').addClass('btn-success').removeClass(packageIdentity);

        packageButton.each(function () {
            if ($(this).hasClass('btn-builder-text')) {
                $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span> Remove from Script Builder');
            } else {
                $(this).html('<span class="fas fa-minus-circle" alt="Remove from Script Builder"></span>');
            }
        });
        packageButton.removeClass('btn-success').addClass('btn-danger').addClass(packageIdentity);

        // Package List
        // Remove old version
        $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).prev().remove();
        $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).remove();

        // Add new version
        builderStorage.append('<div id="' + packageIdentity + '" class="d-flex flex-row align-items-start storage-row ' + packageIdentity + '"><img class="package-image mr-2" src="' + packageImagePath + '' + packageImage + '" height="30" width="30"><div class="mr-2"><a class="text-dark btn-link mb-0 h5 text-break" href="/packages/' + packageUrl + '/' + packageVersion + '">' + packageTitle + '</a><p class="mb-0"><small>' + packageVersion + '</small></p></div><button class="btn btn-sm btn-builder-remove btn-danger ml-auto" value="' + packageValue + '" title="' + packageTitle + '" version="' + packageVersion + '" image="' + packageImage + '"><span class="fas fa-minus-circle"></span></button></div');
        $('<hr />').insertBefore('.storage #' + packageIdentity + '');

        removePackages();
    });

    $('#warning-version').on('hidden.bs.modal', function () {
        $('.btn-builder.active').removeClass('active');
    });

    // Build Command Scripts
    builderModal.on('shown.bs.modal', function () {
        $('code[class^="command-builder-"]').empty();
        builderIndividualScript();
        builderOrganizationScript();
        builderEnvironmentScript();
        $('.copy-command .toolbar a').each(function () {
            var copyCommand = $(this).parentsUntil('.code-toolbar').parent().find('code').attr('class').split(" ");
            $(this).addClass('btn-copy').attr('data-clipboard-target', '.' + copyCommand[0]);
        });
    });

    builderStep3.on('shown.bs.tab', function () {
        $('code.command-builder-individual').empty();
        builderIndividualScript();
    });

    builderStep4.find('a[data-toggle="pill"]').on('shown.bs.tab', function () {
        $('code[class^="command-builder-environment"]').empty()
        builderEnvironmentScript();
    });

    builderStep5.find('a[data-toggle="pill"]').on('shown.bs.tab', function () {
        $('code[class^="command-builder-organization"]').empty();
        builderOrganizationScript();
    });

    $('.btn-bulk-package-download').click(function (e) {
        e.preventDefault();
        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageVersion = getStorage[1];
            storageValue = getStorage[3];

            window.open('https://chocolatey.org/api/v2/package/' + storageValue + '/' + storageVersion);
        }
    });

    // Build new xml doc based on local storage package values
    $('.btn-xml').click(function () {
        var xmlDoc = document.implementation.createDocument(null, "packages");
        (new XMLSerializer()).serializeToString(xmlDoc);

        var parser = new DOMParser();
        prolog = '<?xml version="1.0" encoding="utf-8"?>';

        // Add prolog
        newXmlStr = prolog + (new XMLSerializer()).serializeToString(xmlDoc);
        var xml = parser.parseFromString(newXmlStr, "application/xml");

        // Build xml & add each package node
        var packagesObject = xml.getElementsByTagName("packages");

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageVersion = getStorage[1];
            storageValue = getStorage[3];
            // Creates a new package entry for each item in builder
            var packageNode = xml.createElement("package");
            packagesObject[0].appendChild(packageNode);

            // Determine attributes if either version or pre-release was specified
            if (storageValue.indexOf("--") >= 0) {
                if (storageValue.indexOf("--pre") >= 0) { // If a pre-release
                    storageVersion = storageValue.substr(storageValue.indexOf('--version') + 9).trim();
                    storageVersion = storageVersion.substr(0, storageVersion.indexOf('--')).trim();
                    storageValue = storageValue.substr(0, storageValue.indexOf('--')).trim();
                } else { // If version was specified and is not a pre-release
                    storageValue = storageValue.substr(0, storageValue.indexOf('--')).trim();
                }
                // Set attributes
                packageNode.setAttribute("id", storageValue);
                packageNode.setAttribute("version", storageVersion);
            } else { // No version specified and not a pre-release
                packageNode.setAttribute("id", storageValue);
            }
        }

        // Get xml doc as string
        var text = (new XMLSerializer()).serializeToString(xml);

        // Send off to beautify
        formatXml(text);
    });

    function builderEnvironmentScript() {
        internalRepoUrl = builderScriptInput.val() || "http://internal/odata/repo";
        var commandEnvironmentOne = $('.command-builder-environment-one');
        var commandEnvironmentTwo = $('.command-builder-environment-two');

        commandEnvironmentOne.append("choco download <span></span>--internalize --source=https://chocolatey.org/api/v2");
        commandEnvironmentTwo.append("choco push --source =\"'" + internalRepoUrl + "'\"");

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageValue = getStorage[3];

            // Command Environment One
            commandEnvironmentOne.find('span').append(storageValue + " ");
        }

        // Highlight Command Environment One & Two
        commandEnvironmentOne.add(commandEnvironmentTwo).addClass('language-powershell');
        Prism.highlightElement(commandEnvironmentOne[0]);
        Prism.highlightElement(commandEnvironmentTwo[0]);
    }

    function builderOrganizationScript() {
        internalRepoUrl = builderScriptInput.val() || "http://internal/odata/repo";
        var commandGenericOne = $('.command-builder-organization-generic-one');
        var commandGenericTwo = $('.command-builder-organization-generic-two');
        var commandAnsible = $('.command-builder-organization-ansible');
        var commandChef = $('.command-builder-organization-chef');
        var commandOtter = $('.command-builder-organization-otter');
        var commandPSDSC = $('.command-builder-organization-psdsc');
        var commandPuppet = $('.command-builder-organization-puppet');
        var commandSalt = $('.command-builder-organization-salt');

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageVersion = getStorage[1];
            storageValue = getStorage[3];

            // Command Generic One
            commandGenericOne.append(
                "choco upgrade " + storageValue + " -y --source=\"'" + internalRepoUrl + "'\" [other options]\n"
            )

            // Command Generic Two
            commandGenericTwo.append(
                "choco upgrade " + storageValue + " -y --source=\"'" + internalRepoUrl + "'\" [other options]\n" +
                "$exitCode = $LASTEXITCODE\n\n" +
                "Write-Verbose \"Exit code was $exitCode\"\n" +
                "$validExitCodes = @@(0, 1605, 1614, 1641, 3010)\n" +
                "if ($validExitCodes -contains $exitCode) {\n" +
                "  Exit 0\n" +
                "}\n\n" +
                "Exit $exitCode\n\n"
            )

            // Command Ansible
            commandAnsible.append(
                "- name: Ensure " + storageValue + " installed\n" +
                "  win_chocolatey:\n" +
                "    name: " + storageValue + "\n" +
                "    state: present\n" +
                "    version: " + storageVersion + "\n" +
                "    source: " + internalRepoUrl + "\n\n"
            )

            // Command Chef
            commandChef.append(
                "chocolatey_package '" + storageValue + "' do\n" +
                "  action    :install\n" +
                "  version   '" + storageVersion + "'\n" +
                "  source    '" + internalRepoUrl + "'\n" +
                "end\n\n"
            )

            // Command Otter
            commandOtter.append(
                "Chocolatey::Ensure-Package\n" +
                "(\n" +
                "    Name: " + storageValue + "\n" +
                "    Version: " + storageVersion + "\n" +
                "    Source: " + internalRepoUrl + "\n" +
                ");\n\n"
            )

            // Command PS DSC
            commandPSDSC.append(
                "cChocoPackageInstaller " + storageValue + "{\n" +
                "   Name    = '" + storageValue + "'\n" +
                "   Ensure  = 'Present'\n" +
                "   Version = '" + storageVersion + "'\n" +
                "   Source  = '" + internalRepoUrl + "'\n" +
                "}\n\n"
            )

            // Command Puppet
            commandPuppet.append(
                "package { '" + storageValue + "':\n" +
                "  provider => '" + storageValue + "',\n" +
                "  ensure   => '" + storageVersion + "',\n" +
                "  source   => '" + internalRepoUrl + "',\n" +
                "}\n\n"
            )

            // Command Salt
            commandSalt.append(
                "salt '*' chocolatey.install " + storageValue + " version=\"" + storageVersion + "\" source=\"" + internalRepoUrl + "\"\n"
            )
        }

        // Highlight Command Generic One
        commandGenericOne.addClass('language-powershell');
        Prism.highlightElement(commandGenericOne[0]);

        // Highlight Command Generic Two
        commandGenericTwo.addClass('language-powershell');
        Prism.highlightElement(commandGenericTwo[0]);

        // Highlight Command Ansible
        commandAnsible.addClass('language-yaml');
        Prism.highlightElement(commandAnsible[0]);

        // Highlight Command Chef
        commandChef.addClass('language-ruby');
        Prism.highlightElement(commandChef[0]);

        // Highlight Command Otter
        commandOtter.addClass('language-powershell');
        Prism.highlightElement(commandOtter[0]);

        // Highlight Command PS DSC
        commandPSDSC.addClass('language-powershell');
        Prism.highlightElement(commandPSDSC[0]);

        // Highlight Command Puppet
        commandPuppet.addClass('language-puppet');
        Prism.highlightElement(commandPuppet[0]);

        // Highlight Command Salt
        commandSalt.addClass('language-powershell');
        Prism.highlightElement(commandSalt[0]);
    }

    function builderIndividualScript() {
        var commandIndividual = $('.command-builder-individual');

        for (var i in packages) {
            getStorage = packages[i].split(" , ");
            storageValue = getStorage[3];

            // Command Individual
            commandIndividual.append('choco install ' + storageValue + ' -y\n');
        }

        // Highlight Command Individual
        commandIndividual.addClass('language-poweshell');
        Prism.highlightElement(commandIndividual[0]);
    }

    // Storage
    function removeStorage() {
        if (packages.length < 1) {
            localStorage.removeItem('packageList');
            builderStorage.empty();
            $('#addToBuilder').collapse('hide');
            builderViewBtn.addClass('d-none');
            builderModal.modal('hide');
            removeBuilderInputError();
        }
    }

    // Count items
    function countPackages() {
        builderViewBtn.find('.notification-badge').empty(); // First delete value there
        if (packages.length > 0) {
            builderViewBtn.removeClass('d-none').find('.notification-badge').append(JSON.parse(localStorage.packageList).length); // Add in new value
        }
    }

    // Button click inside of builder list
    function removePackages() {
        $('.btn-builder-remove').click(function () {
            /*$this = $(this);
            $title = $this.attr('title');
            $class = $title.replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, '');
            $value = $this.attr('value');
            $version = $this.attr('version');
            $image = /[^/]*$/.exec($this.parent().parent().find(".package-image").attr('src'))[0];*/
      
            packageTitle = $(this).attr('title'),
            packageValue = $(this).attr('value'),
            packageIdentity = packageTitle.replace(/[^a-z0-9\s]/gi, '').replace(/[_\s]/g, ''),
            packageVersion = $(this).attr('version'),
            packageImage = $(this).attr('image');


            // Delete & Update Title & Version from Storage
            for (var i in packages) {
                if (packages[i] == packageTitle + " , " + packageVersion + " , " + packageImage + " , " + packageValue) {
                    packages.splice(i, 1);
                }
            }
            localStorage.packageList = JSON.stringify(packages);

            //Change button state back to green on main page
            $(this).parentsUntil('body').parent().find(".btn-danger." + packageIdentity).each(function () {
                if ($(this).hasClass('btn-builder-text')) {
                    $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span> Add to Script Builder');
                } else {
                    $(this).html('<span class="fas fa-plus-circle" alt="Add to Script Builder"></span>');
                }
            });
            $(this).parentsUntil('body').parent().find(".btn-danger." + packageIdentity).removeClass('btn-danger').addClass('btn-success').removeClass(packageIdentity);

            // Remove from builder list
            $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).prev().remove();
            $(this).parentsUntil('body').parent().find('.storage').find($('.' + packageIdentity)).remove();

            // Storage
            removeStorage();

            // Count items
            countPackages();
        });
    }

    // Download xml file
    function download(filename, text) {
        var element = document.createElement('a');
        element.setAttribute('href', 'data:text/xml;charset=utf-8,' + encodeURIComponent(text));
        element.setAttribute('download', filename);
        element.style.display = 'none';
        document.body.appendChild(element);
        element.click();
        document.body.removeChild(element);
    }

    // Beautify xml document
    function formatXml(text) {
        var filename = "packages.config"
        formatted = '', indent = '',
            tab = '\t';

        text.split(/>\s*</).forEach(function (node) {
            if (node.match(/^\/\w/)) indent = indent.substring(tab.length); // decrease indent by one 'tab'
            formatted += indent + '<' + node + '>\r\n';
            if (node.match(/^<?\w[^>]*[^\/]$/)) indent += tab;              // increase indent
        });
        text = formatted.substring(1, formatted.length - 3);

        // Send to download
        download(filename, text);
    }

    builderModal.on('shown.bs.modal', function () {
        builderScriptType();
        builderTabs();
        builderNavButtons();

        builderModal.find('.nav-tabs-install a[data-toggle="tab"]').on('shown.bs.tab', function () {
            builderScriptType();
            builderTabs();
        });

        builderScriptInput.keyup(function () {
            builderTabs();
            builderNavButtons();
        }).keyup();

        builderModal.find('.nav-item.disabled').click(function () {
            builderInputError();
            builderNavButtons();
        });

        builderModal.find('#builder-steps a[data-toggle="pill"]').on('shown.bs.tab', function () {
            builderNavButtons();
        });
    });

    builderNextBtn.click(function () {
        builderNextStep = $('#builder-steps').find('.active').parent().next('li').find('a');

        if (builderNextStep.hasClass('disabled') && builderOrganization.hasClass('active')) {
            builderInputError();
        } else if (!builderStep3.hasClass('active') || !builderIndividual.hasClass('active')) {
            builderNextStep.tab('show');
        }

        builderNavButtons();
    });

    builderPrevBtn.click(function () {
        builderPrevStep = $('#builder-steps').find('.active').parent().prev('li').find('a');

        builderPrevStep.tab('show');
        builderNavButtons();
    });

    function builderNavButtons() {
        builderNextStep = $('#builder-steps').find('.active').parent().next('li').find('a');
        builderPrevStep = $('#builder-steps').find('.active').parent().prev('li').find('a');

        // Next Button
        if (builderNextStep.hasClass('disabled') || builderStep5.children().hasClass('active') || builderStep3.hasClass('active') && builderIndividual.hasClass('active')) {
            builderNextBtn.addClass('disabled');
        } else {
            builderNextBtn.removeClass('disabled');
        }

        // Prev Button
        if (builderStep1.hasClass('active')) {
            builderPrevBtn.addClass('disabled');
        } else {
            builderPrevBtn.removeClass('disabled');
        }
    }

    function builderScriptType() {
        if ($('#builder-individual-tab').hasClass('active')) {
            builderStep3.html('<strong><span class="d-none d-sm-inline-block mr-1">STEP</span><span>3</span></strong><p class="mb-0 d-none d-lg-block">Install Script / Config</p>').removeClass('error');
            builderStep4.add(builderStep5).add(builderOrganization).addClass('d-none').children();
            builderIndividual.removeClass('d-none').addClass('active');
            builderOrganization.removeClass('active');
        } else {
            builderStep3.html('<strong><span class="d-none d-sm-inline-block mr-1">STEP</span><span>3</span></strong><p class="mb-0 d-none d-lg-block">Internal Repo Url</p>');
            builderStep4.add(builderStep5).add(builderOrganization).removeClass('d-none').children();
            builderIndividual.addClass('d-none').removeClass('active');
            builderOrganization.addClass('active');
        }
    }

    function builderTabs() {
        if (builderScriptInput.val() == 0) {
            builderStep4.add(builderStep5).addClass('disabled').children().addClass('disabled');
        } else {
            removeBuilderInputError();
        }
    }

    function builderInputError() {
        if (builderScriptInput.val() == 0) {
            builderStep3.addClass('error');
            builderStep3.tab('show');
            if (!builderScriptInput.hasClass('is-invalid')) {
                builderModal.find(builderScriptInput).addClass('is-invalid');
                $('<div class="invalid-feedback">You must enter your internal repository url to continue.</div>').insertAfter(builderModal.find(builderScriptInput));
            }
        }
    }

    function removeBuilderInputError() {
        builderStep4.add(builderStep5).removeClass('disabled').children().removeClass('disabled');
        builderStep3.removeClass('error');
        builderModal.find(builderScriptInput).removeClass('is-invalid');
        builderModal.find('.invalid-feedback').remove();
    }
});