module.exports = {
  preset: "ts-jest",
  roots: ["<rootDir>/src", "<rootDir>/tests"],
  testEnvironment: "jsdom",
  moduleFileExtensions: ["ts", "tsx", "js", "jsx"],
  transform: {
    "^.+\\.(ts|tsx)$": "ts-jest"
  },
  transformIgnorePatterns: [
    "/node_modules/(?!axios)/"
  ]
};
