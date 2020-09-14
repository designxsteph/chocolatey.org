const gulp = require("gulp"),
    del = require("del"),
    concat = require('gulp-concat'),
    cleanCSS = require('gulp-clean-css'),
    purgecss = require("gulp-purgecss"),
    sass = require("gulp-sass"),
    uglify = require("gulp-uglify"),
    pump = require("pump"),
    zipfiles = require("gulp-zip"),
    markdownPdf = require('gulp-markdown-pdf'),
    split = require("split"),
    through = require("through"),
    duplexer = require("duplexer"),
    rename = require('gulp-rename'),
    dist = "Content/dist";
    sass.compiler = require('node-sass');

const { series, parallel, src, dest } = require('gulp');

function clean() {
    return del([dist, "chocolatey-styleguide.zip"]);
}

function compileSASS() {
    return src(["Content/scss/*.scss", "!Content/scss/chocolatey-pdf.scss"])
        .pipe(sass().on('error', sass.logError))
        .pipe(dest(dist));
}

function purge() {
    return src("Content/dist/chocolatey.css")
        .pipe(purgecss({
            content: ["Views/**/*.cshtml", "App_Code/ViewHelpers.cshtml", "Errors/*.*", "Scripts/custom.js", "Scripts/packages/package-details.js", "Content/scss/_search.scss", "Scripts/easymde/easymde.min.js"]
        }))
        .pipe(dest("Content/dist/tmp"));
}

function optimize() {
    return src(["Content/dist/tmp/chocolatey.css", "Content/dist/purge.css"])
        .pipe(concat("chocolatey.slim.css"))
        .pipe(cleanCSS({
            level: 1,
            compatibility: 'ie8'
        }))
        .pipe(dest(dist))
        .on('end', function () {
            del(["Content/dist/purge.css", "Content/dist/tmp"]);
        });
}

// Styleguide Zip File Process

// First copy files
function copyFonts() {
    return src("Content/fonts/*.*")
        .pipe(gulp.dest("styleguide/fonts"));
}
function copyCSS() {
    return src(["Content/prism/prism.css", "Content/dist/chocolatey.css", "Content/dist/chocolatey.slim.css"])
        .pipe(dest("styleguide/css"));
}
function copyTmpJS() {
    return src(["Scripts/*.js"])
        .pipe(dest("styleguide/tmp"));
}
function copyJS() {
    return src("Scripts/prism/prism.js")
        .pipe(dest("styleguide/js"));
}

// Second optimize CSS
function cssStyleguide() {
    return src("styleguide/css/*.css")
        .pipe(cleanCSS({
            level: 1,
            compatibility: 'ie8'
        }))
        .pipe(dest("styleguide/css"));
}

// Next concat JS files in temp folder
function jsStyleguideConcat() {
    return src([
        "styleguide/tmp/jquery-3.5.1.js",
        "styleguide/tmp/bootstrap.bundle.js",
        "styleguide/tmp/clipboard.js",
        "styleguide/tmp/custom.js"])
        .pipe(concat("chocolatey.js"))
        .pipe(dest("styleguide/js"))
        .on('end', function () {
            del("styleguide/tmp");
        });
}

// Then Optimize JS
function jsStyleguide(cb) {
    pump([
        src("styleguide/js/*.js"),
        uglify(),
        dest("styleguide/js")
    ],
        cb
    );
}

// Zip it all up and delete temporary styleguide folder
function zip() {
    return src("styleguide/*/*.*")
        .pipe(zipfiles("chocolatey-styleguide.zip"))
        .pipe(dest("./"))
        .on('end', function () {
            del(["styleguide", "Content/dist/chocolatey.css"]);
        });
}

// Docs to PDF
function cleanDocs() {
    return del(["../../Temp_PDF", "Content/Pdf/*.css"], { force: true });
}

function pdfCss() {
    return src("Content/scss/chocolatey-pdf.scss")
        .pipe(sass().on('error', sass.logError))
        .pipe(purgecss({
            content: ["Views/Documentation/*.cshtml", "Views/Documentation/Files/*.md", "Scripts/custom.js", "Scripts/bootstrap.bundle.js", "Content/scss/_print.scss"]
        }))
        .pipe(cleanCSS({
            level: 1,
            compatibility: 'ie8'
        }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(dest("Content/Pdf"))
}

function preProcessMd() {
    var splitter = split({ trailing: false })
    var docsUrl = "https://chocolatey.org/docs/";
    var urlOne = /\[\[((?:(?!\[\[).)*?)\|(.*?)]]/g;
    var urlTwo = /(\[(.*?)\])\(#(.*?)\)/g;
    var urlThree = /\[(.*?)\]\((.*?)\)/g;
    var urlImg = /(\()(images)/g;
    var multilineComment = /^\s*<!--\s*$[^]*?^\s*-->\s*$/gm;
    var replacer = through(function (data) {
        this.queue(
            data
                .replace(urlOne, (_, x, y) => `[${x}](${docsUrl}${y.replace(/(?!^)[A-Z]/g, '-$&').toLowerCase()})`)
                .replace(urlTwo, "$2")
                .replace(urlThree, (_, x, y) => `[${x}](${y.replace(/f-a-q/g, "faq").replace(/p-s1/g, "ps1").replace(/---/g, '-').replace(/--/g, '-').toLowerCase()})`)
                .replace(urlImg, "$1$2".replace("$2", "content/images/docs"))
                .replace(multilineComment, "")
            + "\n"
        )
    })
    
    splitter.pipe(replacer)
        .on('error', function (err) {
            console.log(err)
        })
    return duplexer(splitter, replacer)
}

function docsToPdf() {
    return src(["Views/Documentation/Files/*.md", "!Views/Documentation/Files/_README.md"])
        .pipe(markdownPdf({
            preProcessMd: preProcessMd,
            remarkable: {
                html: true
            },
            paperBorder: "1cm",
            runningsPath: "Content/Pdf/runnings.js",
            cssPath: "Content/Pdf/chocolatey-pdf.min.css"
        }))
        .pipe(rename(function (path) {
            path.basename = path.basename
                .replace("ChocolateyFAQs", "ChocolateyFaqs")
                .replace(/piKey/g, "pikey")
                .replace(/MyGet/g, "Myget")
                .replace(/(^|[\s-])\S/g, function (match) { return match.toUpperCase() }) // Capitolize letter after a -
                .replace(/-/g, "") // Remove -
                .replace(/(?!^)[A-Z]/g, '-$&') // Add - before capitol letter
                .replace(/P-S1/g, "ps1")
                .toLowerCase();
            console.log("Processed: " + path.basename + ".pdf");
        }))
        .pipe(dest("../../Temp_PDF"))
        .on('end', function () {
            console.log("Complete! Files are located in chocolatey.org\\Temp_PDF\\. Please delete this folder after uploading.")
        })
}

// Tasks
exports.build = series(clean, compileSASS, purge, optimize);
exports.createZip = series(copyFonts, copyCSS, copyTmpJS, copyJS, cssStyleguide, jsStyleguideConcat, jsStyleguide, zip);
exports.convertDocsToPdf = series(cleanDocs, pdfCss, docsToPdf);


// Default Task
exports.default = series(exports.build, exports.createZip);