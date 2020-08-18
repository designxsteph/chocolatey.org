const gulp = require('gulp'),
    del = require('del'),
    concat = require('gulp-concat'),
    purgeCss = require('gulp-purgecss'),
    sass = require('gulp-sass'),
    uglify = require('gulp-uglify-es').default,
    rename = require('gulp-rename'),
    pump = require('pump'),
    zipfiles = require('gulp-zip'),
    cleanCss = require('gulp-clean-css'),
    cssDist = 'Content/dist',
    jsDist = 'Scripts/dist',
    fonts = 'Content/fonts',
    merge = require('merge-stream'),
    bundleconfig = require('./bundleconfig.json');
    sass.compiler = require('node-sass');

const { series, parallel, src, dest } = require('gulp');

const regex = {
    css: /\.css$/,
    js: /\.js$/
};

const getBundles = (regexPattern) => {
    return bundleconfig.filter(bundle => {
        return regexPattern.test(bundle.outputFileName);
    });
};

function clean() {
    return del([cssDist, jsDist, fonts, 'chocolatey-styleguide.zip']);
}

function copyFonts() {
    return src('node_modules/@fortawesome/fontawesome-free/webfonts/*.*')
        .pipe(dest(fonts));
}

function compileSass() {
    return src('Content/scss/*.scss')
        .pipe(sass().on('error', sass.logError))
        .pipe(dest(cssDist));
}

function compileJs() {
    var tasks = getBundles(regex.js).map(function (bundle) {

        return src(bundle.inputFiles, { base: '.' })
            .pipe(concat(bundle.outputFileName))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function compileCss() {
    var tasks = getBundles(regex.css).map(function (bundle) {

        return src(bundle.inputFiles, { base: '.' })
            .pipe(concat(bundle.outputFileName))
            .pipe(purgeCss({
                content: [
                    'Views/**/**/*.cshtml',
                    'Scripts/dist/*.js',
                    'App_Code/ViewHelpers.cshtml',
                    'Errors/*.*',
                    'Content/scss/_search.scss',
                    'Content/scss/_purge.scss'
                ]
            }))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function minCss() {
    var tasks = getBundles(regex.css).map(function (bundle) {

        return src(bundle.outputFileName, { base: '.' })
            .pipe(cleanCss({
                level: 2,
                compatibility: 'ie8'
            }))
            .pipe(rename({ suffix: '.min' }))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function minJs() {
    var tasks = getBundles(regex.js).map(function (bundle) {

        return src(bundle.outputFileName, { base: '.' })
            .pipe(uglify())
            .pipe(rename({ suffix: '.min' }))
            .pipe(dest('.'));
    });

    return merge(tasks);
}

function cleanEnd() {
    return del([
        cssDist + '/*.css',
        '!' + cssDist + '/*.min.css',
        jsDist + '/*.js',
        '!' + jsDist + '/*.min.js',
        fonts + '/fa-regular-400.*',
    ]);
}

// Styleguide Zip File Process
// First copy files
function copyCssZip() {
    return src([cssDist + '/*.css'])
        .pipe(dest('styleguide/css'));
}

function copyJsZip() {
    return src(jsDist + '/*.js')
        .pipe(dest('styleguide/js'));
}

function copyFontsZip() {
    return src(fonts + '/*.*')
        .pipe(dest('styleguide/fonts'));
}

// Zip it all up
function zip() {
    return src('styleguide/*/*.*')
        .pipe(zipfiles('chocolatey-styleguide.zip'))
        .pipe(dest('./'))
        .on('end', function () {
            del(['styleguide']);
        });
}

// Gulp series
exports.compileSassJs = parallel(compileSass, compileJs);
exports.minCssJs = parallel(minCss, minJs);
exports.createZip = series(copyFontsZip, copyCssZip, copyJsZip, zip);

// Gulp default
exports.default = series(clean, copyFonts, exports.compileSassJs, compileCss, exports.minCssJs, cleanEnd, exports.createZip);