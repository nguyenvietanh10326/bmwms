import re

sql_file = r'c:\Users\Dumb Trung\Documents\GitHub\bmwms\BMWMS_Database_ToBe_v3.0.sql'
with open(sql_file, 'r', encoding='utf-8') as f:
    sql = f.read()

tables = {}
parts = sql.split('CREATE TABLE')
for part in parts[1:]:
    header = part.split('(', 1)
    if len(header) < 2:
        continue
    t_name = header[0].replace('dbo.', '').replace('[', '').replace(']', '').strip()
    
    content = header[1]
    depth = 1
    end_idx = -1
    for i, char in enumerate(content):
        if char == '(': depth += 1
        elif char == ')': depth -= 1
        if depth == 0:
            end_idx = i
            break
            
    if end_idx != -1:
        columns_text = content[:end_idx]
        columns = []
        for line in columns_text.split('\n'):
            line = line.strip().strip(',')
            if not line or line.startswith('--') or line.startswith('CONSTRAINT') or line.startswith('PRIMARY KEY') or line.startswith('FOREIGN KEY') or line.startswith('UNIQUE') or line.startswith('CHECK'):
                continue
            
            col_match = re.match(r'^\[?(\w+)\]?\s+\[?([\w\s]+)\]?', line)
            if col_match:
                col_name = col_match.group(1)
                col_type = col_match.group(2).split()[0].replace(' ', '')
                is_pk = 'PRIMARY KEY' in line.upper() or 'IDENTITY' in line.upper()
                columns.append({'name': col_name, 'type': col_type, 'is_pk': is_pk})
        
        tables[t_name] = columns

# Extract FKs
fks = []
# Match: CONSTRAINT FK_... FOREIGN KEY (Col) REFERENCES Table(Col)
fk_pattern_1 = re.compile(r'FOREIGN KEY\s*\(\[?(\w+)\]?\)\s*REFERENCES\s+(?:dbo\.)?\[?(\w+)\]?', re.IGNORECASE)
for part in parts[1:]:
    header = part.split('(', 1)
    if len(header) >= 2:
        t_name = header[0].replace('dbo.', '').replace('[', '').replace(']', '').strip()
        for match in fk_pattern_1.finditer(header[1]):
            fks.append({'from_table': t_name, 'to_table': match.group(2)})

# Match ALTER TABLE
fk_pattern_2 = re.compile(r'ALTER TABLE\s+(?:dbo\.)?\[?(\w+)\]?.*?FOREIGN KEY\s*\(\[?(\w+)\]?\)\s*REFERENCES\s+(?:dbo\.)?\[?(\w+)\]?', re.IGNORECASE | re.DOTALL)
for match in fk_pattern_2.finditer(sql):
    fks.append({'from_table': match.group(1), 'to_table': match.group(3)})

# Remove duplicates
unique_fks = []
seen = set()
for fk in fks:
    t = (fk['from_table'], fk['to_table'])
    if t not in seen:
        seen.add(t)
        unique_fks.append(fk)

mmd = 'erDiagram\n'
for t_name, cols in tables.items():
    mmd += f'    {t_name} {{\n'
    for c in cols:
        pk = ' PK' if c['is_pk'] else ''
        mmd += f'        {c["type"]} {c["name"]}{pk}\n'
    mmd += '    }\n'

for fk in unique_fks:
    mmd += f'    {fk["to_table"]} ||--o{{ {fk["from_table"]} : "" \n'

with open(r'c:\Users\Dumb Trung\Documents\GitHub\bmwms\db_diagram.mmd', 'w', encoding='utf-8') as f:
    f.write(mmd)

print(f"Generated db_diagram.mmd with {len(tables)} tables and {len(unique_fks)} relationships")
