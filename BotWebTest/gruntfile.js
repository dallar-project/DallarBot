/// <binding AfterBuild='default' />
/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    'use strict';

    const sass = require('node-sass');

    // Project configuration.
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),

        // Sass
        sass: {
            options: {
                implementation: sass,
                sourceMap: true, // Create source map
                outputStyle: 'compressed' // Minify output
            },
            dist: {
                files: [
                    {
                        expand: true, // Recursive
                        cwd: "Styles", // The startup directory
                        src: ["**/*.scss"], // Source files
                        dest: "wwwroot/css", // Destination
                        ext: ".css" // File extension
                    }
                ]
            }
        }
    });

    // Load the plugin
    grunt.loadNpmTasks('grunt-sass');

    // Default task(s).
    grunt.registerTask('default', ['sass']);
};