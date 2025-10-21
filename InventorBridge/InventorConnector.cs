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

        /// <summary>
        /// Modifica ANCHO, LARGO, LARGOF (pulgadas) y guarda en outputDirectory.
        /// appearanceKey (opcional): si no es null/empty, intenta aplicar esa apariencia.
        /// </summary>
        public void ModifyTankSheetMetal(
            string partPath,
            LeftWall tank,
            double flangeIn,
            string outputDirectory,
            string appearanceKey
        )
        {
            if (string.IsNullOrWhiteSpace(partPath))
                throw new ArgumentException("Ruta de la pieza no válida.", "partPath");
            if (!IOFile.Exists(partPath))
                throw new System.IO.FileNotFoundException("No se encontró la pieza a modificar.", partPath);
            if (flangeIn <= 0)
                throw new ArgumentOutOfRangeException("flangeIn", "El largo de pestaña (LARGOF) debe ser > 0 in.");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Directorio de salida no válido.", "outputDirectory");

            // Asegurar carpeta de salida
            IODirectory.CreateDirectory(outputDirectory);
            string destPath = IOPath.Combine(outputDirectory, IOPath.GetFileName(partPath));

            PartDocument partDoc = (PartDocument)_invApp.Documents.Open(partPath, true);
            if (partDoc == null) throw new InvalidOperationException("No se pudo abrir el documento.");

            Transaction txn = _invApp.TransactionManager.StartTransaction(
                (Inventor._Document)partDoc, "TankBuilder: Modify (ANCHO/LARGO/LARGOF/Apariencia)");

            try
            {
                SheetMetalComponentDefinition smDef =
                    partDoc.ComponentDefinition as SheetMetalComponentDefinition;
                if (smDef == null)
                    throw new InvalidOperationException("El documento no es de Sheet Metal.");

                // Asignación directa (en pulgadas)
                double altoIn = tank.ALTO;     // ALTO
                double largoIn = tank.LARGO;    // LARGO
                double largofIn = flangeIn;       // LARGOF (pestaña)
                //double flangeAng = 0;

                bool okAlto = TrySetInterpolatedParameter(partDoc, "ALTO", altoIn, "in");
                bool okLargo = TrySetInterpolatedParameter(partDoc, "LARGO", largoIn, "in");
                bool okLargof = TrySetInterpolatedParameter(partDoc, "FLANGEIN", largofIn, "in");

                if (!okAlto) throw new InvalidOperationException("Parámetro 'ALTO' no encontrado en la pieza.");
                if (!okLargo) throw new InvalidOperationException("Parámetro 'LARGO' no encontrado en la pieza.");
                if (!okLargof) throw new InvalidOperationException("Parámetro 'LARGOF' (pestaña) no encontrado en la pieza.");

                // Apariencia opcional (solo si mandan una clave no vacía)
                if (!string.IsNullOrWhiteSpace(appearanceKey))
                {
                    ApplyAppearanceIfPossible(partDoc, appearanceKey);
                }

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

        // Firma compatibilidad (sin apariencia explícita)
        public void ModifyTankSheetMetal(string partPath, LeftWall tank, string outputDirectory)
        {
            double flangeIn = (tank != null && tank.FLANGEIN > 0) ? tank.FLANGEIN : -1;
            if (flangeIn <= 0)
                throw new NotSupportedException(
                    "Se requiere el largo de pestaña (LARGOF). Use ModifyTankSheetMetal(partPath, tank, flangeIn, outputDirectory).");

            ModifyTankSheetMetal(partPath, tank, flangeIn, outputDirectory, null);
        }

        // ======== APARIENCIA (opcional) ========
        private void ApplyAppearanceIfPossible(PartDocument doc, string appearanceKey)
        {
            // Construir candidatos según la clave "humana"
            string[] candidates;

            if (string.IsNullOrWhiteSpace(appearanceKey))
                return;

            string key = appearanceKey.Trim().ToLowerInvariant();

            if (key == "gris")
            {
                candidates = new string[]
                {
                    "Generic - Gray", "Gray", "Paint - Enamel Glossy (Gray)",
                    "Steel - Satin", "Aluminum - Brushed", "Dark Gray"
                };
            }
            else if (key == "verde" || key == "verde (sstl)" || key == "verde sstl")
            {
                candidates = new string[]
                {
                    "Dark Green"
                };
            }
            else
            {
                // usar el nombre tal cual recibimos
                candidates = new string[] { appearanceKey };
            }

            Asset found = FindAppearanceAssetByNames(candidates);
            if (found != null)
            {
                doc.ActiveAppearance = found;
            }
            // si no encontró, no romper: continuar sin cambio
        }

        private Asset FindAppearanceAssetByNames(string[] names)
        {
            if (names == null || names.Length == 0) return null;

            AssetLibraries libs = _invApp.AssetLibraries;
            foreach (AssetLibrary lib in libs)
            {
                foreach (string n in names)
                {
                    try
                    {
                        Asset a = lib.AppearanceAssets[n];
                        if (a != null) return a;
                    }
                    catch
                    {
                        // Nombre no existe en esta librería; continuar
                    }
                }
            }
            return null;
        }

        // ======== HELPERS ========
        private static bool TrySetInterpolatedParameter(PartDocument doc, string target, double value, string units)
        {
            if (doc == null || string.IsNullOrWhiteSpace(target)) return false;

            Parameters parms = doc.ComponentDefinition.Parameters;
            string t = NormalizeName(target);

            // 1) Exacta (normalizada)
            foreach (Parameter p in parms.UserParameters)
            {
                if (NormalizeName(p.Name) == t)
                {
                    p.Expression = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + units;
                    return true;
                }
            }
            foreach (ModelParameter mp in parms.ModelParameters)
            {
                if (NormalizeName(mp.Name) == t)
                {
                    mp.Expression = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + units;
                    return true;
                }
            }

            // 2) Contiene (normalizada)
            foreach (Parameter p in parms.UserParameters)
            {
                if (NormalizeName(p.Name).Contains(t))
                {
                    p.Expression = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + units;
                    return true;
                }
            }
            foreach (ModelParameter mp in parms.ModelParameters)
            {
                if (NormalizeName(mp.Name).Contains(t))
                {
                    mp.Expression = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + units;
                    return true;
                }
            }

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
