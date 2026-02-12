const sql = require('mssql');
const fs = require('fs');
const path = require('path');

const config = {
    user: 'db_aaf0a8_diaxcrm_admin',
    password: 'Alexandre10#',
    server: 'sql1002.site4now.net',
    database: 'db_aaf0a8_diaxcrm',
    options: {
        encrypt: true,
        trustServerCertificate: true
    }
};

async function run() {
    try {
        await sql.connect(config);

        const result = {};

        // 1. Accounts
        console.log("Fetching accounts...");
        const accounts = await sql.query`SELECT id, name FROM financial_accounts WHERE name LIKE '%Santander%' OR name LIKE '%Ita%'`;
        result.accounts = accounts.recordset;

        // 2. Specific Income Check
        console.log("Fetching incomes...");
        const incomeCheck = await sql.query`SELECT id, description, date, created_at, created_by FROM incomes WHERE description LIKE '%ALEXAND%' ORDER BY created_at DESC`;
        result.incomes = incomeCheck.recordset;

        // 3. Specific Expense Check
        console.log("Fetching expenses...");
        // Select expenses for found accounts
        if (result.accounts.length > 0) {
            const accIds = result.accounts.map(a => `'${a.id}'`).join(',');
            // We use simple string injection here as per previous logic (safe for this specific internal script usage with UUIDs)
             const expenseCheck = await sql.query(`SELECT TOP 20 id, description, date, created_at, financial_account_id FROM expenses WHERE financial_account_id IN (${accIds}) ORDER BY created_at DESC`);
             result.expenses = expenseCheck.recordset;
        } else {
             const expenseCheck = await sql.query`SELECT TOP 20 id, description, date, created_at, financial_account_id FROM expenses ORDER BY created_at DESC`;
             result.expenses = expenseCheck.recordset;
        }

        // 4. Imported Transactions Check
        console.log("Fetching imported transactions...");
        const imported = await sql.query`SELECT TOP 20 id, raw_description, transaction_date, status, created_income_id, created_expense_id FROM imported_transactions ORDER BY created_at DESC`;
        result.imported = imported.recordset;

        fs.writeFileSync(path.join(__dirname, 'output.json'), JSON.stringify(result, null, 2));
        console.log("Done writing output.json");

    } catch (err) {
        console.error("Error:", err);
    } finally {
        await sql.close();
    }
}

run();
