PATCH RhSensoWeb (DataTables + chamada API /api/v1/botoes)

O QUE ESTE PACOTE CONTÉM
------------------------
Somente os arquivos alterados/novos do projeto Web (src/RhSensoWeb):

- Program.cs
- Areas/SEG/Controllers/BotoesController.cs
- Areas/SEG/Views/Botoes/Index.cshtml

O QUE ELE FAZ
-------------
1) Program.cs registra um HttpClient nomeado "Api" com base em Api:BaseUrl
   (de appsettings.json) e injeta o bearer token do cookie/Session "AuthToken".

2) BotoesController expõe GET /seg/botoes/data (via método GetData) que
   traduz os parâmetros do DataTables para a API GET /api/v1/botoes?sistema=...

3) Index.cshtml cria a tabela com DataTables (server-side) e filtros "sistema"
   e "funcao". Os assets (jQuery + DataTables) vêm via CDN.

COMO APLICAR
------------
1) Faça backup da pasta src/RhSensoWeb do seu projeto.
2) Extraia este zip sobrepondo os arquivos dentro de src/RhSensoWeb.
3) Garanta que appsettings.json (ou secrets) tenham:
   "Api": { "BaseUrl": "https://SEU-HOST-DA-API/" }

4) Build e execute RhSensoWeb. Abra /seg/botoes.

OBS.:
- Se preferir usar seus assets locais, remova os CDN da view e use os seus bundles.
- A API precisa responder em /api/v1/botoes conforme Swagger (parâmetros: sistema, funcao, orderBy, asc, page, pageSize, search).
