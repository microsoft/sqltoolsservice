set DOTNETCONFIG=-c Integration

cmd /c npm install
node ./node_modules/gulp/bin/gulp.js
