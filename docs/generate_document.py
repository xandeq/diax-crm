import os
import re
import sys


def parse_text_to_html(text):
    lines = text.strip().split('\n')
    html_parts = []

    # Assume first line is title
    if lines:
        title = lines[0].strip()
        html_parts.append(f'<div class="topline"><div class="doc-title" style="font-size: 18pt; font-weight: 700; color: var(--primary); text-transform: uppercase; margin-bottom: 5px;">{title}</div></div><hr />')
        lines = lines[1:]

    in_list = False

    for line in lines:
        line = line.strip()
        if not line:
            if in_list:
                html_parts.append('</ul>')
                in_list = False
            continue

        # Check for headers (1. Title)
        header_match = re.match(r'^(\d+\.)\s+(.+)', line)
        if header_match:
            if in_list:
                html_parts.append('</ul>')
                in_list = False
            html_parts.append(f'<div class="section"><h2>{line}</h2></div>')
            continue

        # Check for list items (• or -)
        if line.startswith('•') or line.startswith('-'):
            content = line[1:].strip()
            if not in_list:
                html_parts.append('<ul>')
                in_list = True
            html_parts.append(f'<li>{content}</li>')
            continue

        # Normal paragraph
        if in_list:
            html_parts.append('</ul>')
            in_list = False

        html_parts.append(f'<p>{line}</p>')

    if in_list:
        html_parts.append('</ul>')

    return '\n'.join(html_parts)

def generate_document(input_path, output_path, template_path):
    try:
        with open(input_path, 'r', encoding='utf-8') as f:
            text_content = f.read()

        with open(template_path, 'r', encoding='utf-8') as f:
            template_content = f.read()

        html_content = parse_text_to_html(text_content)

        # Extract title for <title> tag
        title_match = text_content.strip().split('\n')[0] if text_content else "Documento"

        final_html = template_content.replace('{{CONTENT}}', html_content)
        final_html = final_html.replace('{{TITLE}}', title_match)

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(final_html)

        print(f"Successfully generated {output_path}")

    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    # Default paths
    base_dir = os.path.dirname(os.path.abspath(__file__))
    input_file = os.path.join(base_dir, 'briefing.txt')
    output_file = os.path.join(base_dir, 'briefing.html')
    template_file = os.path.join(base_dir, 'template.html')

    if len(sys.argv) > 1:
        input_file = sys.argv[1]
    if len(sys.argv) > 2:
        output_file = sys.argv[2]

    generate_document(input_file, output_file, template_file)
