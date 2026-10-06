import { expect, Page, test } from "@playwright/test";

// A merchant using the demo page. Each test is one flow, end to end through the real gateway
// and the bank simulator: the simulator answers from the card's last digit
// (odd authorizes, even declines, 0 means the bank is down).

const paymentsTable = (page: Page) => page.locator("#payments");
const result = (page: Page) => page.locator("#result");

async function pickScenario(page: Page, name: string) {
  await page.getByRole("group", { name: "Scenarios" }).getByRole("button", { name }).click();
}

async function pay(page: Page) {
  const response = page.waitForResponse(r => r.url().endsWith("/api/v1/payments") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Pay" }).click();
  return response;
}

test.beforeEach(async ({ page }) => {
  await page.goto("/");
});

test("should authorize a payment and retrieve it from the list", async ({ page }) => {
  await pickScenario(page, "Authorized");
  await expect(page.getByLabel("Amount (minor units)")).toHaveValue("1050");
  await expect(page.locator("#amount-hint")).toHaveText("= 10.50 GBP");

  const response = await pay(page);
  expect(response.status()).toBe(200);
  const payment = await response.json();

  await expect(result(page).locator(".badge")).toHaveText("Authorized");
  await expect(result(page)).toContainText("POST /api/v1/payments → 200");

  // Only the last four digits come back; the full card number and the CVV never do.
  expect(payment.cardNumberLastFour).toBe("8877");
  await expect(result(page)).not.toContainText("2222405343248877");
  await expect(result(page)).not.toContainText("\"cvv\"");

  const row = paymentsTable(page).getByRole("row").filter({ hasText: payment.id });
  await expect(row).toContainText("Authorized");
  await expect(row).toContainText("•••• 8877");
  await expect(row).toContainText("10.50 GBP");

  // The merchant later looks the payment up and gets the same record back.
  const lookup = page.waitForResponse(`**/api/v1/payments/${payment.id}`);
  await row.getByRole("button", { name: "Retrieve" }).click();
  expect((await lookup).status()).toBe(200);
  await expect(result(page)).toContainText(`GET /api/v1/payments/${payment.id} → 200`);
  await expect(result(page).locator(".badge")).toHaveText("Authorized");
});

test("should store a declined payment so the merchant can see it", async ({ page }) => {
  await pickScenario(page, "Declined");

  const response = await pay(page);
  expect(response.status()).toBe(200);
  const payment = await response.json();

  await expect(result(page).locator(".badge")).toHaveText("Declined");
  const row = paymentsTable(page).getByRole("row").filter({ hasText: payment.id });
  await expect(row).toContainText("Declined");
  await expect(row).toContainText("600.00 USD");

  await page.getByLabel("Payment id").fill(payment.id);
  await page.locator("#lookup-form").getByRole("button", { name: "Retrieve" }).click();
  await expect(result(page)).toContainText(`GET /api/v1/payments/${payment.id} → 200`);
  await expect(result(page).locator(".badge")).toHaveText("Declined");
});

test("should report the bank as unavailable and store nothing", async ({ page }) => {
  await pickScenario(page, "Bank unavailable");

  const response = await pay(page);
  expect(response.status()).toBe(502);

  await expect(result(page).locator(".badge")).toHaveText("Bank unavailable");
  await expect(result(page)).toContainText("Nothing was stored");
  await expect(paymentsTable(page)).toContainText("Payments you make here will appear in this list.");
});

test("should reject an invalid payment with every error and never call the bank", async ({ page }) => {
  await pickScenario(page, "Invalid (rejected)");

  const response = await pay(page);
  expect(response.status()).toBe(400);
  const body = await response.json();

  expect(body.status).toBe("Rejected");
  expect(Object.keys(body.errors).sort()).toEqual(["amount", "cardNumber", "currency", "cvv", "expiryMonth"]);
  await expect(result(page).locator(".badge")).toHaveText("Rejected");
  await expect(paymentsTable(page)).toContainText("Payments you make here will appear in this list.");
});

test("should let the merchant fix a rejected payment and resubmit it", async ({ page }) => {
  await pickScenario(page, "Authorized");
  await page.getByLabel("Currency").selectOption("JPY");

  expect((await pay(page)).status()).toBe(400);
  await expect(result(page)).toContainText("Currency must be one of: GBP, USD, EUR.");

  await page.getByLabel("Currency").selectOption("EUR");
  const response = await pay(page);
  expect(response.status()).toBe(200);
  await expect(result(page).locator(".badge")).toHaveText("Authorized");
  await expect(paymentsTable(page).getByRole("row")).toHaveCount(1); // only the successful attempt is stored
});

test("should say not found for an unknown payment id", async ({ page }) => {
  const unknownId = "00000000-0000-0000-0000-000000000000";

  await page.getByLabel("Payment id").fill(unknownId);
  await page.locator("#lookup-form").getByRole("button", { name: "Retrieve" }).click();

  await expect(result(page).locator(".badge")).toHaveText("Not found");
  await expect(result(page)).toContainText(`GET /api/v1/payments/${unknownId} → 404`);
});
