#MENU HTML 
Debido a que el menú se basa en un archivo XML, se tiene que acceder atraves de un servidor local (por seguridad),
por tanto antes de intentar abrir el arcivo se deben ejecutar por orden los siguientes comandos (se da por hecho que el que lo intenta abrir tiene instlado python).
Los comandos son los siguientes y en una terminal cmd no powershell:
  - cd a la carpeta donde hallas guradado el HTML
  - python3 -m http.server 8000
  - Y en el navegador buscas server:800 hay selecionas el archivo HTML y listo 
