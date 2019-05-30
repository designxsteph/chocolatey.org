const gulp = require("gulp"),
    del = require("del"),
    concat = require('gulp-concat'),
    cleanCSS = require('gulp-clean-css'),
    purgecss = require("gulp-purgecss"),
    sass = require("gulp-sass"),
    uglify = require("gulp-uglify"),
    pump = require("pump"),
    zipfiles = require("gulp-zip"),
    dist = "Content/dist";
    sass.compiler = require('node-sass');

function clean() {
    return del([dist, "chocolatey-styleguide.zip"]);
}

function compileSASS() {
    return gulp.src("Content/scss/*.scss")
        .pipe(sass().on('error', sass.logError))
        .pipe(gulp.dest(dist));
}

function purge() {
    return gulp.src("Content/dist/chocolatey.css")
        .pipe(purgecss({
            content: ["Views/**/*.cshtml", "App_Code/ViewHelpers.cshtml", "Errors/*.*", "Scripts/custom.js", "Scripts/packages/package-details.js", "Content/scss/_search.scss", "Scripts/easymde/easymde.min.js", "Scripts/add-to-builder/add-to-builder.js"]
        }))
        .pipe(gulp.dest("Content/dist/tmp"));
}

function optimize() {
    return gulp.src(["Content/dist/tmp/chocolatey.css", "Content/dist/purge.css"])
        .pipe(concat("chocolatey.slim.css"))
        .pipe(cleanCSS({
            level: 1,
            compatibility: 'ie8'
        }))
        .pipe(gulp.dest(dist))
        .on('end', function () {
            del(["Content/dist/purge.css", "Content/dist/tmp"]);
        });
}

// Styleguide Zip File Process

// First copy files
function copyFonts() {
    return gulp.src("Content/fonts/*.*")
        .pipe(gulp.dest("styleguide/fonts"));
}
function copyCSS() {
    return gulp.src(["Content/prism/prism.css", "Content/dist/chocolatey.css", "Content/dist/chocolatey.slim.css"])
        .pipe(gulp.dest("styleguide/css"));
}
function copyTmpJS() {
    return gulp.src(["Scripts/*.js"])
        .pipe(gulp.dest("styleguide/tmp"));
}
function copyJS() {
    return gulp.src("Scripts/prism/prism.js")
        .pipe(gulp.dest("styleguide/js"));
}

// Second optimize CSS
function cssStyleguide() {
    return gulp.src("styleguide/css/*.css")
        .pipe(cleanCSS({
            level: 1,
            compatibility: 'ie8'
        }))
        .pipe(gulp.dest("styleguide/css"));
}

// Next concat JS files in temp folder
function jsStyleguideConcat() {
    return gulp.src([
        "styleguide/tmp/jquery-3.5.1.js",
        "styleguide/tmp/bootstrap.bundle.js",
        "styleguide/tmp/clipboard.js",
        "styleguide/tmp/custom.js"])
        .pipe(concat("chocolatey.js"))
        .pipe(gulp.dest("styleguide/js"))
        .on('end', function () {
            del("styleguide/tmp");
        });
}

// Then Optimize JS
function jsStyleguide(cb) {
    pump([
        gulp.src("styleguide/js/*.js"),
        uglify(),
        gulp.dest("styleguide/js")
    ],
        cb
    );
}

// Zip it all up and delete temporary styleguide folder
function zip() {
    return gulp.src("styleguide/*/*.*")
        .pipe(zipfiles("chocolatey-styleguide.zip"))
        .pipe(gulp.dest("./"))
        .on('end', function () {
            del(["styleguide", "Content/dist/chocolatey.css"]);
        });
}

// Task
gulp.task("default", gulp.series(clean, compileSASS, purge, optimize, copyFonts, copyCSS, copyTmpJS, copyJS, cssStyleguide, jsStyleguideConcat, jsStyleguide, zip));