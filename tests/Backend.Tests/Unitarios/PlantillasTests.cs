using Backend.Modulos.Sistemas.GeneradorBackend;

namespace Backend.Tests.Unitarios
{
    /// <summary>Plantillas del backend generado (recursos embebidos en Backend.dll).</summary>
    public class PlantillasTests
    {
        [Fact]
        public void Todas_las_plantillas_quedan_embebidas_en_la_dll_principal()
        {
            // Ojo: "Program.cs.plantilla" sin WithCulture="false" se iría a un satélite "cs" (checo).
            var carpeta = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "backend", "Modulos", "Sistemas", "GeneradorBackend", "Plantillas");
            var enDisco = Directory.GetFiles(carpeta, "*.plantilla").Select(f => "Plantillas/" + Path.GetFileName(f));
            var embebidas = typeof(Plantillas).Assembly.GetManifestResourceNames();

            Assert.NotEmpty(enDisco);
            Assert.All(enDisco, nombre => Assert.Contains(nombre, embebidas));
        }

        [Fact]
        public void Completa_los_marcadores_en_una_sola_pasada()
        {
            var texto = Plantillas.Leer("launchSettings.json", ("proyecto", "{{puerto}}"), ("puerto", 6001));

            Assert.Contains("\"{{puerto}}\": {", texto);
            Assert.Contains("http://localhost:6001", texto);
        }

        [Fact]
        public void Falla_si_falta_un_marcador() =>
            Assert.Throws<InvalidOperationException>(() => Plantillas.Leer("launchSettings.json", ("proyecto", "X")));
    }
}
