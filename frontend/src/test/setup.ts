import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";

URL.createObjectURL = vi.fn(() => "blob:shortcut-preview");
URL.revokeObjectURL = vi.fn();

Object.defineProperty(navigator, "clipboard", {
  configurable: true,
  value: {
    writeText: vi.fn(() => Promise.resolve())
  }
});
