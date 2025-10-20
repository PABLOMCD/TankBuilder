using System;
using System.Runtime.InteropServices;
using Inventor;
using CoreLogic.Models;

// ===== Aliases para evitar ambigüedad con Inventor.Path / Inventor.File =====
using IOFile = global::System.IO.File;
using IOPath = global::System.IO.Path;
using IODirectory = global::System.IO.Directory;

namespace InventorBridge
{
    public class InventorConnector
    {
        private Inventor.Application _invApp;

        public InventorConnector()
        {
            try { _invApp = (Inventor.Application)Marshal.BindToMoniker("!Inventor.Application"); }
            catch
            {
                var t = Type.GetTypeFromProgID("Inventor.Application");
                _invApp = (Inventor.Application)Activator.CreateInstance(t);
                _invApp.Visible = true;
            }
        }

        // ========= MODIFICAR PIEZA EXISTENTE Y GUARDAR EN OTRA CARPETA =========
        /// <summary>
        /// Modifica ANCHO, LARGO y LARGOF (pulgadas) y guarda la pieza resultante en outputDirectory
        /// conservando el nombre del archivo original.
        /// </summary>
        public void ModifyTankSheetMetal(string partPath, TankModel tank, double flangeIn, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(partPath))
                throw new ArgumentException("Ruta de la pieza no válida.", nameof(partPath));
            if (!IOFile.Exists(partPath))
                throw new System.IO.FileNotFoundException("No se encontró la pieza a modificar.", partPath);
            if (flangeIn <= 0)
                throw new ArgumentOutOfRangeException(nameof(flangeIn), "El largo de pestaña (LARGOF) debe ser > 0 in.");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Directorio de salida no válido.", nameof(outputDirectory));

            // Asegurar carpeta de salida (usar alias para evitar ambigüedad)
            IODirectory.CreateDirectory(outputDirectory);

            // Construir ruta destino con alias de Path
            string destPath = IOPath.Combine(outputDirectory, IOPath.GetFileName(partPath));

            var partDoc = (PartDocument)_invApp.Documents.Open(partPath, true)
                ?? throw new InvalidOperationException("No se pudo abrir el documento.");

            var txn = _invApp.TransactionManager.StartTransaction(
                (Inventor._Document)partDoc, "TankBuilder: Modify SheetMetal (ANCHO/LARGO/LARGOF)");

            try
            {
                var smDef = partDoc.ComponentDefinition as SheetMetalComponentDefinition
                    ?? throw new InvalidOperationException("El documento no es de Sheet Metal.");

                // Asignación directa (en pulgadas)
                double anchoIn = tank.Width;     // ANCHO
                double largoIn = tank.Height;    // LARGO
                double largofIn = flangeIn;       // LARGOF (pestaña)

                bool okAncho = TrySetInterpolatedParameter(partDoc, "ALTO", anchoIn, "in");
                bool okLargo = TrySetInterpolatedParameter(partDoc, "LARGO", largoIn, "in");
                bool okLargof = TrySetInterpolatedParameter(partDoc, "LARGOF", largofIn, "in");

                if (!okAncho) throw new InvalidOperationException("Parámetro 'ANCHO' no encontrado en la pieza.");
                if (!okLargo) throw new InvalidOperationException("Parámetro 'LARGO' no encontrado en la pieza.");
                if (!okLargof) throw new InvalidOperationException("Parámetro 'LARGOF' (pestaña) no encontrado en la pieza.");

                partDoc.Update2(true);

                // Guardar como NUEVO archivo en la carpeta destino
                partDoc.SaveAs(destPath, false);

                txn.End();
            }
            catch
            {
                txn.Abort();
                throw;
            }
        }

        // Firma anterior (compatibilidad): ahora exige también outputDirectory
        public void ModifyTankSheetMetal(string partPath, TankModel tank, string outputDirectory)
        {
            double flangeIn = (tank?.Depth ?? 0) > 0 ? tank.Depth : -1;
            if (flangeIn <= 0)
                throw new NotSupportedException(
                    "Se requiere el largo de pestaña (LARGOF). Use ModifyTankSheetMetal(partPath, tank, flangeIn, outputDirectory).");

            ModifyTankSheetMetal(partPath, tank, flangeIn, outputDirectory);
        }

        // ========= HELPERS =========
        private static bool TrySetInterpolatedParameter(PartDocument doc, string target, double value, string units)
        {
            if (doc == null || string.IsNullOrWhiteSpace(target)) return false;

            var parms = doc.ComponentDefinition.Parameters;
            string t = NormalizeName(target);

            // 1) Coincidencia exacta (normalizada)
            foreach (Parameter p in parms.UserParameters)
                if (NormalizeName(p.Name) == t) { p.Expression = $"{value} {units}"; return true; }

            foreach (ModelParameter mp in parms.ModelParameters)
                if (NormalizeName(mp.Name) == t) { mp.Expression = $"{value} {units}"; return true; }

            // 2) Coincidencia parcial (contiene)
            foreach (Parameter p in parms.UserParameters)
                if (NormalizeName(p.Name).Contains(t)) { p.Expression = $"{value} {units}"; return true; }

            foreach (ModelParameter mp in parms.ModelParameters)
                if (NormalizeName(mp.Name).Contains(t)) { mp.Expression = $"{value} {units}"; return true; }

            return false;
        }

        private static string NormalizeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Trim().ToLowerInvariant()
                    .Replace("_", "").Replace("-", "").Replace(" ", "")
                    .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                    .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        }
    }
}
