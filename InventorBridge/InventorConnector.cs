using System;
using System.IO;
using System.Runtime.InteropServices;
using Inventor;
using CoreLogic.Models;
using SysEnv = global::System.Environment;
using IOFile = global::System.IO.File;

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

        public void ModifyTankSheetMetal(string partPath, TankModel tank, double flangeIn)
        {
            if (string.IsNullOrWhiteSpace(partPath))
                throw new ArgumentException("Ruta de la pieza no válida.", nameof(partPath));
            if (!IOFile.Exists(partPath))
                throw new FileNotFoundException("No se encontró la pieza a modificar.", partPath);
            if (flangeIn <= 0)
                throw new ArgumentOutOfRangeException(nameof(flangeIn), "El largo de pestaña (LARGOF) debe ser > 0 in.");

            var partDoc = (PartDocument)_invApp.Documents.Open(partPath, true)
                ?? throw new InvalidOperationException("No se pudo abrir el documento.");

            var txn = _invApp.TransactionManager.StartTransaction(
                (Inventor._Document)partDoc, "TankBuilder: Modify SheetMetal");

            try
            {
                var smDef = partDoc.ComponentDefinition as SheetMetalComponentDefinition
                    ?? throw new InvalidOperationException("El documento no es de Sheet Metal.");

                // === Actualizar parámetros (en pulgadas) ===
                double anchoIn = tank.Width;
                double largoIn = tank.Height;
                double largofIn = flangeIn;

                bool okAncho = TrySetInterpolatedParameter(partDoc, "ALTO", anchoIn, "in");
                bool okLargo = TrySetInterpolatedParameter(partDoc, "LARGO", largoIn, "in");
                bool okLargof = TrySetInterpolatedParameter(partDoc, "LARGOF", largofIn, "in");

                if (!okAncho) throw new InvalidOperationException("Parámetro 'ANCHO' no encontrado.");
                if (!okLargo) throw new InvalidOperationException("Parámetro 'LARGO' no encontrado.");
                if (!okLargof) throw new InvalidOperationException("Parámetro 'LARGOF' no encontrado.");

                partDoc.Update2(true);
                partDoc.Save();
                txn.End();
            }
            catch
            {
                txn.Abort();
                throw;
            }
        }

        // =======================
        //  HELPERS
        // =======================
        private static bool TrySetInterpolatedParameter(PartDocument doc, string target, double value, string units)
        {
            if (doc == null || string.IsNullOrWhiteSpace(target)) return false;
            var parms = doc.ComponentDefinition.Parameters;
            string t = NormalizeName(target);

            foreach (Parameter p in parms.UserParameters)
                if (NormalizeName(p.Name) == t || NormalizeName(p.Name).Contains(t))
                { p.Expression = $"{value} {units}"; return true; }

            foreach (ModelParameter mp in parms.ModelParameters)
                if (NormalizeName(mp.Name) == t || NormalizeName(mp.Name).Contains(t))
                { mp.Expression = $"{value} {units}"; return true; }

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
