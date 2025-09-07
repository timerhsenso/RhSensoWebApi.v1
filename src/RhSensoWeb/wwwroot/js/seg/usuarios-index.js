(function () {
  const grid = document.querySelector("#grid tbody");
  const selAll = document.getElementById("selAll");
  const chkInativos = document.getElementById("chkInativos");
  const btnResetMany = document.getElementById("btnResetMany");

  function rowTemplate(x) {
    return `<tr>
      <td><input type="checkbox" class="sel" data-id="${x.codigo}"/></td>
      <td>${x.codigo}</td>
      <td>${x.descricao}</td>
      <td>${x.tipoStr}</td>
      <td>${x.email ?? ""}</td>
      <td>${x.situacao}</td>
      <td>${x.noUser}</td>
      <td>${x.cdEmpresa ?? ""}</td>
      <td>${x.cdFilial ?? ""}</td>
      <td class="text-end"><a class="btn btn-sm btn-outline-primary" href="/SEG/Usuarios/Form?id=${x.codigo}">Editar</a></td>
    </tr>`;
  }

  async function load() {
    const exibirInativos = chkInativos.checked;
    const res = await fetch(`/SEG/Usuarios/GetData?exibirInativos=${exibirInativos}`);
    const json = await res.json();
    grid.innerHTML = "";
    for (const r of json.data) grid.insertAdjacentHTML("beforeend", rowTemplate(r));
  }

  selAll?.addEventListener("change", () => {
    document.querySelectorAll("#grid .sel").forEach(cb => cb.checked = selAll.checked);
  });

  btnResetMany?.addEventListener("click", async () => {
    const ids = Array.from(document.querySelectorAll("#grid .sel:checked")).map(cb => cb.dataset.id);
    if (!ids.length) { alert("Selecione ao menos um usuário."); return; }
    if (!confirm("Redefinir senha para a padrão dos usuários selecionados?")) return;
    const res = await fetch(`/SEG/Usuarios/ResetPasswords`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(ids) });
    if (res.ok) { alert("Senhas redefinidas."); } else { alert("Falha ao redefinir."); }
  });

  chkInativos?.addEventListener("change", load);
  load();
})();