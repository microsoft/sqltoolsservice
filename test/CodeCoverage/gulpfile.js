var gulp = require('gulp');
//var install = require('gulp-install');;
var del = require('del');
var request = require('request');
var fs = require('fs');
var gutil = require('gulp-util');
var through = require('through2');
var cproc = require('child_process');
var os = require('os');

function nugetRestoreArgs(nupkg, options) {
    var args = new Array();
    if (os.platform() != 'win32') {
        args.push('./nuget.exe');
    }
    args.push('restore');
    args.push(nupkg);

    var withValues = [
        'source',
        'configFile',
        'packagesDirectory',
        'solutionDirectory',
        'msBuildVersion'
    ];

    var withoutValues = [
        'noCache',
        'requireConsent',
        'disableParallelProcessing'
    ];

    withValues.forEach(function(prop) {
        var value = options[prop];
        if(value) {
            args.push('-' + prop);
            args.push(value);
        }
    });

    withoutValues.forEach(function(prop) {
        var value = options[prop];
        if(value) {
            args.push('-' + prop);
        }
    });

    args.push('-noninteractive');

    return args;
};

function nugetRestore(options) {
    options = options || {};
    options.nuget = options.nuget || './nuget.exe';
    if (os.platform() != 'win32') {
        options.nuget = 'mono';
    }

    return through.obj(function(file, encoding, done) {
        var args = nugetRestoreArgs(file.path, options);
        cproc.execFile(options.nuget, args, function(err, stdout) {
            if (err) {
                throw new gutil.PluginError('gulp-nuget', err);
            }

            gutil.log(stdout.trim());
            done(null, file);
        });
    });
};

gulp.task('ext:nuget-download', function(done) {
    if(fs.existsSync('nuget.exe')) {
        return done();
    }

    request.get('http://nuget.org/nuget.exe')
        .pipe(fs.createWriteStream('nuget.exe'))
        .on('close', done);
});

gulp.task('ext:nuget-restore', function() {

    var options = {
      configFile: './nuget.config',
      packagesDirectory: './packages'
    };

    return gulp.src('./packages.config')
        .pipe(nugetRestore(options));
});


gulp.task('ext:code-coverage', function(done) {
    cproc.execFile('cmd.exe', [ '/c', 'codecoverage.bat' ], function(err, stdout) {
        if (err) {
            throw new gutil.PluginError('ext:code-coverage', err);
        }

        gutil.log(stdout.trim());
    });
    return done();
});

gulp.task('test', gulp.series('ext:nuget-download', 'ext:nuget-restore', 'ext:code-coverage'));

gulp.task('default', gulp.series('test'));
