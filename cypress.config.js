const { defineConfig } = require('cypress');

module.exports = defineConfig({
  e2e: {
    baseUrl: 'http://localhost:3000',
    specPattern: 'tests/frontend/e2e/**/*.cy.js',
    fixturesFolder: 'tests/frontend/e2e/fixtures',
    supportFile: false,
    video: false,
    screenshotOnRunFailure: true,
    // Reduce server connection timeouts for CI environments
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 10000
  },
});