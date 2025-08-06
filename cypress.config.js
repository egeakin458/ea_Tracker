const { defineConfig } = require('cypress');

module.exports = defineConfig({
  e2e: {
    baseUrl: 'http://localhost:3000',
    specPattern: 'tests/frontend/e2e/**/*.cy.js',
    fixturesFolder: 'tests/frontend/e2e/fixtures',
    supportFile: false,
    video: true,
    screenshotOnRunFailure: true
  },
});