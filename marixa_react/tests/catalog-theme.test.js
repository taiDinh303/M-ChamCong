import test from "node:test";
import assert from "node:assert/strict";
import { initialTheme } from "../src/services/theme/theme.js";
import { recordPayload } from "../src/modules/catalog/catalogSchema.js";
test("theme keeps explicit preference and safely falls back when storage fails", () => {
  assert.equal(initialTheme({ getItem: () => "light" }, true), "light");
  assert.equal(initialTheme({ getItem: () => null }, true), "dark");
  assert.equal(
    initialTheme(
      {
        getItem: () => {
          throw Error("denied");
        },
      },
      false,
    ),
    "light",
  );
  assert.equal(initialTheme({ getItem: () => "invalid" }, true), "dark");
});
test("catalog payload preserves nullable leave allowance, business date and boolean values", () => {
  assert.equal(
    recordPayload("LeaveType", { maxDays: null, isPaid: false }).maxDays,
    null,
  );
  assert.deepEqual(
    recordPayload("HolidayCalendar", { date: "2027-01-01", type: "3" }),
    { date: "2027-01-01", type: 3, year: 2027 },
  );
  assert.deepEqual(
    recordPayload("EmployeeContract", {
      contractType: "2",
      endDate: "",
      lastUpdatedTime: "token",
    }),
    { contractType: 2, endDate: null },
  );
});
