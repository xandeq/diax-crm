# -*- coding: utf-8 -*-
"""DIAX CRM DB Maintenance Script.
Purges old audit_logs and app_logs to keep the SmarterASP 500MB limit safe.
Run weekly (or whenever DB usage > 70%).

Usage:
  python db_maintenance.py [--dry-run] [--keep-days 30]

Credentials: read from DIAX_DB_CONNSTRING in:
  1. ~/.claude/.secrets.env  (central secrets file)
  2. .env in the same directory as this script
  3. DIAX_DB_CONNSTRING environment variable
"""
import os, sys, pyodbc, datetime
from pathlib import Path


def _load_env_file(path: Path) -> dict:
    """Parse a simple KEY=VALUE .env file, ignore comments and blank lines."""
    result = {}
    if not path.exists():
        return result
    for line in path.read_text(encoding='utf-8').splitlines():
        line = line.strip()
        if not line or line.startswith('#') or '=' not in line:
            continue
        key, _, val = line.partition('=')
        result[key.strip()] = val.strip()
    return result


def _get_conn_str() -> str:
    # Priority: env var → local .env → ~/.claude/.secrets.env
    val = os.environ.get('DIAX_DB_CONNSTRING')
    if val:
        return val
    local_env = _load_env_file(Path(__file__).parent / '.env')
    val = local_env.get('DIAX_DB_CONNSTRING')
    if val:
        return val
    central = _load_env_file(Path.home() / '.claude' / '.secrets.env')
    val = central.get('DIAX_DB_CONNSTRING')
    if val:
        return val
    raise RuntimeError(
        'DIAX_DB_CONNSTRING not found. '
        'Add it to ~/.claude/.secrets.env or set the environment variable.'
    )


CONN_STR = _get_conn_str()

def ts():
    return datetime.datetime.now().strftime('%H:%M:%S')

def main():
    dry_run = '--dry-run' in sys.argv
    keep_days = 30
    for i, a in enumerate(sys.argv):
        if a == '--keep-days' and i + 1 < len(sys.argv):
            keep_days = int(sys.argv[i + 1])

    print(f'[{ts()}] DIAX DB Maintenance (dry_run={dry_run}, keep_days={keep_days})')

    conn = pyodbc.connect(CONN_STR, timeout=30)
    conn.autocommit = True
    cur = conn.cursor()

    # Check current DB size
    cur.execute('SELECT SUM(size)*8/1024 AS size_mb FROM sys.database_files')
    size_mb = cur.fetchone()[0]
    print(f'[{ts()}] Current DB size: {size_mb} MB')

    cutoff = datetime.datetime.utcnow() - datetime.timedelta(days=keep_days)
    cutoff_str = cutoff.strftime('%Y-%m-%d')

    # Count rows to delete
    cur.execute('SELECT COUNT(*) FROM audit_logs WHERE timestamp_utc < ?', cutoff_str)
    audit_to_del = cur.fetchone()[0]
    cur.execute('SELECT COUNT(*) FROM app_logs WHERE timestamp_utc < ?', cutoff_str)
    app_to_del = cur.fetchone()[0]

    print(f'[{ts()}] audit_logs to delete (>{keep_days}d old): {audit_to_del}')
    print(f'[{ts()}] app_logs to delete (>{keep_days}d old):   {app_to_del}')

    if dry_run:
        print(f'[{ts()}] DRY RUN — no changes made')
        conn.close()
        return

    # Delete audit_logs in batches
    total_audit = 0
    while True:
        cur.execute('DELETE TOP(5000) FROM audit_logs WHERE timestamp_utc < ?', cutoff_str)
        n = cur.rowcount
        total_audit += n
        if n < 5000:
            break
    print(f'[{ts()}] Deleted {total_audit} audit_log rows')

    # Delete app_logs in batches — column is timestamp_utc (#2 batch, #3 exact name)
    total_app = 0
    while True:
        cur.execute('DELETE TOP(5000) FROM app_logs WHERE timestamp_utc < ?', cutoff_str)
        n = cur.rowcount
        total_app += n
        if n < 5000:
            break
    print(f'[{ts()}] Deleted {total_app} app_log rows')

    # Shrink DB — use logical file name instead of fixed file_id (#5)
    print(f'[{ts()}] Shrinking database...')
    cur.execute('DBCC SHRINKDATABASE (db_aaf0a8_diaxcrm, 10) WITH NO_INFOMSGS')
    for logical_name in ['db_aaf0a8_diaxcrm_Data', 'db_aaf0a8_diaxcrm_Log']:
        try:
            cur.execute(f'DBCC SHRINKFILE ({logical_name}, 5) WITH NO_INFOMSGS')
        except Exception:
            pass

    cur.execute('SELECT SUM(size)*8/1024 AS size_mb FROM sys.database_files')
    new_size = cur.fetchone()[0]
    print(f'[{ts()}] New DB size: {new_size} MB (freed {size_mb - new_size} MB)')
    conn.close()
    print(f'[{ts()}] DONE')

if __name__ == '__main__':
    main()
