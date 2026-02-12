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

const SANTANDER_ID = '822B36D6-86FD-4D08-94A6-3C973C45EE77';
const ITAU_ID = 'FB6A29C1-56E0-41A2-B4E2-DFA1C3A6A611';

async function run() {
    try {
        await sql.connect(config);
        console.log("Connected to SQL Server");

        const result = {};

        // 1. Verify Incomes
        console.log("Verifying Incomes...");
        const incomes = await sql.query`
            SELECT id, description, date
            FROM incomes
            WHERE description LIKE '%ALEXAND%'
            ORDER BY date
        `;
        result.incomes = incomes.recordset;

        // 2. Verify Expenses
        console.log("Verifying Expenses...");
        const expenses = await sql.query`
            SELECT id, description, date
            FROM expenses
            WHERE financial_account_id IN (${SANTANDER_ID}, ${ITAU_ID})
            AND (description LIKE '%Antonio%' OR description LIKE '%Telefonica%' OR description LIKE '%Pao do Parque%')
            ORDER BY date
        `;
        result.expenses = expenses.recordset;

        fs.writeFileSync(path.join(__dirname, 'verification_output.json'), JSON.stringify(result, null, 2));
        console.log("Done writing verification_output.json");

    } catch (err) {
        console.error("Error:", err);
    } finally {
        await sql.close();
    }
}

run();
