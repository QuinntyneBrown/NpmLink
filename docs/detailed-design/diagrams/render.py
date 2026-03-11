"""Render all .puml files to PNG using the PlantUML web service."""
import os
import glob
import plantuml

server = plantuml.PlantUML(url='http://www.plantuml.com/plantuml/img/')
diagram_dir = os.path.dirname(os.path.abspath(__file__))

for puml_file in sorted(glob.glob(os.path.join(diagram_dir, '*.puml'))):
    basename = os.path.splitext(os.path.basename(puml_file))[0]
    png_file = os.path.join(diagram_dir, f'{basename}.png')
    print(f'Rendering {basename}.puml -> {basename}.png ...', end=' ')
    try:
        success = server.processes_file(puml_file, outfile=png_file)
        if success:
            size = os.path.getsize(png_file)
            print(f'OK ({size} bytes)')
        else:
            print('FAILED')
    except Exception as e:
        print(f'ERROR: {e}')
