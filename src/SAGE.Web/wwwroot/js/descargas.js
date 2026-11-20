window.descargarArchivo = async function (nombre, referencia) {
    const bytes = await referencia.arrayBuffer();
    const url = URL.createObjectURL(new Blob([bytes], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    }));
    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(url);
};
