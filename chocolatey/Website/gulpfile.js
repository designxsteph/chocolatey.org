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

function purge() {
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

function optimize() {
    return gulp.src([dist + "/chocolatey.purge.css", dist + "/purge.css"])
        .pipe(concat("chocolatey.slim.css"))
        .pipe(minifyCss({
            compatibility: 'ie8',
            level: {
                1: {
                    specialComments: 0
                },
                2: {}
            }
        }))
        .pipe(gulp.dest(dist));
        .on('end', function () {
            del([dist + "/purge.css"]);
        });
}

// Task
gulp.task("clean-task", gulp.series(clean));
gulp.task("compileSass-task", gulp.series(compileSass));
gulp.task("purge-task", gulp.series(purge));
gulp.task("optimize-task", gulp.series(optimize));

gulp.task("default", gulp.series(clean, compileSass, purge, optimize));