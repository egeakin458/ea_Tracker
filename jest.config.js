module.exports = {
  displayName: "Frontend Tests",
  preset: "ts-jest",
  testEnvironment: "jsdom",
  roots: ["<rootDir>/tests/frontend", "<rootDir>/src/frontend/src"],
  moduleFileExtensions: ["ts", "tsx", "js", "jsx"],
  transform: {
    "^.+\\.(ts|tsx)$": ["ts-jest", {
      tsconfig: {
        jsx: "react-jsx",
        esModuleInterop: true,
        allowSyntheticDefaultImports: true
      }
    }]
  },
  transformIgnorePatterns: [
    "/node_modules/(?!(axios|@microsoft/signalr)/)"
  ],
  moduleNameMapper: {
    "^axios$": "axios/dist/node/axios.cjs"
  },
  testMatch: [
    "<rootDir>/tests/frontend/unit/**/*.(test|spec).(ts|tsx|js|jsx)",
    "<rootDir>/tests/frontend/integration/**/*.(test|spec).(ts|tsx|js|jsx)"
  ],
  setupFilesAfterEnv: ["<rootDir>/tests/frontend/setup.ts"],
  moduleDirectories: ["node_modules", "<rootDir>/node_modules"]
};