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

        // --- BACKUP ---
        console.log("Creating Backup...");
        const backupData = {};
        backupData.incomes = (await sql.query`SELECT * FROM incomes`).recordset;
        // Filter expenses backup to relevant accounts to save space/time if large
        backupData.expenses = (await sql.query`SELECT * FROM expenses WHERE financial_account_id IN (${SANTANDER_ID}, ${ITAU_ID})`).recordset;

        fs.writeFileSync(path.join(__dirname, 'backup_before_fix.json'), JSON.stringify(backupData, null, 2));
        console.log(`Backup saved to backup_before_fix.json (${backupData.incomes.length} incomes, ${backupData.expenses.length} expenses)`);


        // --- FIX INCOMES ---
        // Swap Day/Month. Only if Day <= 12.
        // SQL: DATEFROMPARTS(YEAR(date), DAY(date), MONTH(date))
        // Verify Day <= 12 logic.

        console.log("Fixing Incomes...");
        const incomesToFix = await sql.query`
            SELECT id, date
            FROM incomes
            WHERE DATEPART(day, date) <= 12
        `;

        let incomeCount = 0;
        for (const income of incomesToFix.recordset) {
            const oldDate = new Date(income.date);
            const year = oldDate.getUTCFullYear();
            const month = oldDate.getUTCMonth() + 1; // JS 0-11
            const day = oldDate.getUTCDate();

            // Swap Month and Day
            // New Month = Old Day
            // New Day = Old Month

            // Construct new ISO string YYYY-MM-DD
            // Pad with 0
            const newMonth = day.toString().padStart(2, '0');
            const newDay = month.toString().padStart(2, '0');
            const newDateStr = `${year}-${newMonth}-${newDay}T00:00:00.000Z`;

            if (day === month) continue; // No change

            // Update
            await sql.query`UPDATE incomes SET date = ${newDateStr} WHERE id = ${income.id}`;
            incomeCount++;
            if (incomeCount % 10 === 0) process.stdout.write('.');
        }
        console.log(`\nUpdated ${incomeCount} Incomes.`);


        // --- FIX EXPENSES ---
        // Swap Day/Month AND Year + 1 (2025 -> 2026).
        // Only for Santander and Itaú.
        // Only if Day <= 12 (Safety, though analysis suggests all are Day=12).

        console.log("Fixing Expenses (Santander & Itaú)...");
        const expensesToFix = await sql.query`
            SELECT id, date
            FROM expenses
            WHERE financial_account_id IN (${SANTANDER_ID}, ${ITAU_ID})
            AND DATEPART(day, date) <= 12
            AND YEAR(date) = 2025
        `; // Added Year=2025 safe guard as user said "2025 -> 2026".

        let expenseCount = 0;
        for (const expense of expensesToFix.recordset) {
            const oldDate = new Date(expense.date);
            const year = oldDate.getUTCFullYear();
            const month = oldDate.getUTCMonth() + 1;
            const day = oldDate.getUTCDate();

            // Swap Month/Day AND Add 1 Year
            const newYear = year + 1;
            const newMonth = day.toString().padStart(2, '0');
            const newDay = month.toString().padStart(2, '0');
            const newDateStr = `${newYear}-${newMonth}-${newDay}T00:00:00.000Z`;

            await sql.query`UPDATE expenses SET date = ${newDateStr} WHERE id = ${expense.id}`;
            expenseCount++;
            if (expenseCount % 10 === 0) process.stdout.write('.');
        }
        console.log(`\nUpdated ${expenseCount} Expenses.`);

    } catch (err) {
        console.error("Error:", err);
    } finally {
        await sql.close();
    }
}

run();
