const gulp = require("gulp"),
    del = require("del"),
    concat = require('gulp-concat'),
    minifyCss = require('gulp-clean-css'),
    purgecss = require("gulp-purgecss"),
    sass = require("gulp-sass"),
    rename = require("gulp-rename"),
    dist = "Content/dist";
    sass.compiler = require('node-sass');

function clean() {
    return del(dist);
}

function compileSass() {
    return gulp.src("Content/scss/*.scss")
        .pipe(sass().on('error', sass.logError))
        .pipe(gulp.dest(dist));
}

function purgeCss() {
    return gulp.src(dist + "/chocolatey.css")
        .pipe(purgecss({
            content: [
                "Views/**/*.cshtml",
                "App_Code/ViewHelpers.cshtml",
                "Errors/*.*",
                "Scripts/custom.js",
                "Scripts/packages/package-details.js",
                "Scripts/easymde/easymde.min.js",
                "Content/scss/_search.scss"
            ]
        }))
        .pipe(rename("chocolatey.purge.css"))
        .pipe(gulp.dest(dist));
}

function concatCss() {
    return gulp.src([dist + "/chocolatey.purge.css", dist + "/purge.css"])
        .pipe(concat("chocolatey.slim.css"))
        .pipe(gulp.dest(dist));
}

function optimizeCss() {
    return gulp.src([
        dist + "/*.css",
        "!" + dist + "/purge.css",
        "!" + dist + "/chocolatey.css",
        "!" + dist + "/chocolatey.purge.css"
    ])
        .pipe(minifyCss({
            compatibility: 'ie8',
            level: {
                1: {
                    specialComments: 0
                },
                2: {}
            }
        }))
        .pipe(rename(function (path) {
            path.basename += ".min";
        }))
        .pipe(gulp.dest(dist));
}

function cleanEnd() {
    return del([dist + "/*.css", "!" + dist + "/*.min.css"]);
}

// Task
gulp.task("clean-task", gulp.series(clean));
gulp.task("compileSass-task", gulp.series(compileSass));
gulp.task("purgeCss-task", gulp.series(purgeCss));
gulp.task("concatCss-task", gulp.series(concatCss));
gulp.task("optimizeCss-task", gulp.series(optimizeCss));
gulp.task("cleanEnd-task", gulp.series(cleanEnd));


gulp.task("default", gulp.series(clean, compileSass, purgeCss, concatCss, optimizeCss, cleanEnd));