'use strict';

const gulp = require('gulp'),
    babel = require('gulp-babel'),
    concat = require('gulp-concat'),
    cleancss = require('gulp-clean-css'),
    uglify = require('gulp-uglify-es').default,
    sass = require('gulp-sass'),
    clean = require('gulp-clean'),
    purgecss = require('gulp-purgecss'),
    rename = require('gulp-rename'),
    merge = require('merge-stream'),
    injectstring = require('gulp-inject-string'),
    bundleconfig = require('./bundleconfig.json');

const editFilePartial = 'Edit this file at https://github.com/chocolatey/choco-theme/partials';
const { series, parallel, src, dest, watch } = require('gulp');
sass.compiler = require('node-sass');

const regex = {
    css: /\.css$/,
    js: /\.js$/
};

const paths = {
    content: 'Content/',
    js: 'Scripts/',
    partials: 'Views/GlobalPartials',
    node_modules: 'node_modules/',
    theme: 'node_modules/choco-theme/'
};

const getBundles = (regexPattern) => {
    return bundleconfig.filter(bundle => {
        return regexPattern.test(bundle.outputFileName);
    });
};

function del() {
    return src([
        paths.content + 'css',
        paths.content + 'fonts',
        paths.content + 'images/global-shared',
        paths.js + '*',
        '!' + paths.js + 'validation',
        '!' + paths.js + 'validation/*.*',
        paths.partials
    ], { allowEmpty: true })
        .pipe(clean({ force: true }));
}

function copyTheme() {
    var copyFontAwesome = src(paths.node_modules + '@fortawesome/fontawesome-free/webfonts/*.*')
        .pipe(dest(paths.content + 'fonts/fontawesome-free'));

    var copyImages = src(paths.theme + 'images/global-shared/*.*')
        .pipe(dest(paths.content + 'images/global-shared'));

    var copyIcons = src(paths.theme + 'images/icons/*.*')
        .pipe(dest('./'));

    var copyPartials = src([paths.theme + 'partials/*.*'])
        .pipe(injectstring.prepend('@* ' + editFilePartial + ' *@\n'))
        .pipe(rename({ prefix: "_", extname: '.cshtml' }))
        .pipe(dest(paths.partials));

    var copyValidationJs = src(paths.theme + 'js/chocolatey-validation.js')
        .pipe(dest(paths.js + 'validation'));

    return merge(copyFontAwesome, copyImages, copyIcons, copyPartials, copyValidationJs);
}

function compileSass() {
    return src(paths.theme + 'scss/*.scss')
        .pipe(sass().on('error', sass.logError))
        .pipe(injectstring.replace('/assets/fonts/fontawesome-free', '/Content/fonts/fontawesome-free'))
        .pipe(dest(paths.content + 'css'));
}

function compileJs() {
    var tasks = getBundles(regex.js).map(function (bundle) {

        return gulp.src(bundle.inputFiles, { base: '.' })
            .pipe(babel({
                "sourceType": "unambiguous",
                "presets": [
                    ["@babel/preset-env", { 
                        "targets": {
                            "ie": "10"
                        }
                    }
                  ]]
            }))
            .pipe(concat(bundle.outputFileName))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function compileCss() {
    var tasks = getBundles(regex.css).map(function (bundle) {

        return gulp.src(bundle.inputFiles, { base: '.' })
            .pipe(concat(bundle.outputFileName))
            .pipe(gulp.dest('.'));
    });

    return merge(tasks);
}

function purgeCss() {
    return src(paths.content + 'css/chocolatey.bundle.css')
        .pipe(purgecss({
            content: [
                'Views/**/*.cshtml',
                'App_Code/ViewHelpers.cshtml',
                'Errors/*.*',
                paths.js + '**/**/*.*',
                paths.theme + 'scss/_gitter.scss'
            ],
            safelist: [
                '::-webkit-scrollbar', 
                '::-webkit-scrollbar-thumb'
            ]
        }))
        .pipe(dest(paths.content + 'css/'));
}

function minCss() {
    var tasks = getBundles(regex.css).map(function (bundle) {

        return gulp.src(bundle.outputFileName, { base: '.' })
            .pipe(cleancss({
                level: 2,
                compatibility: 'ie8'
            }))
            .pipe(rename({ suffix: '.min' }))
            .pipe(gulp.dest('.'));
    });

    return merge(tasks);
}

function minJs() {
    var tasks = getBundles(regex.js).map(function (bundle) {

        return gulp.src(bundle.outputFileName, { base: '.' })
            .pipe(uglify())
            .pipe(rename({ suffix: '.min' }))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function delEnd() {
    return src([
        paths.content + 'css/**/*.css',
        '!' + paths.content + 'css/**/*.min.css',
        paths.js + '**/*.js',
        '!' + paths.js + '**/*.min.js',
        '!' + paths.js + 'validation',
        '!' + paths.js + 'validation/*.*'
    ], { allowEmpty: true })
        .pipe(clean({ force: true }));
}

// Independednt tasks
exports.del = del;

// Gulp series
exports.compileSassJs = parallel(compileSass, compileJs);
exports.minCssJs = parallel(minCss, minJs);

// Gulp default
exports.default = series(del, copyTheme, exports.compileSassJs, compileCss, purgeCss, exports.minCssJs, delEnd);

// Watch files
exports.watchFiles = function () {
    watch([paths.theme], exports.default);
};