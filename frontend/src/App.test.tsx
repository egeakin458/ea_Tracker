
import "@testing-library/jest-dom";
import { render, screen } from "@testing-library/react"; 

import App from "./App";

test("renders investigators dashboard heading", () => {
  render(<App />);
  const heading = screen.getByText(/Investigators/i);
  expect(heading).toBeInTheDocument();
});
