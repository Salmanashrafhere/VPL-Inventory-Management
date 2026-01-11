using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using VPLAssistPlus.Models;

namespace VPLAssistPlus.Data
{
    public class ProductRepository
    {
        private readonly string _filePath;

        public ProductRepository(string filePath)
        {
            _filePath = filePath;
        }

        public List<Product> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<Product>();

                string json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<List<Product>>(json);
                return data ?? new List<Product>();
            }
            catch
            {
                // if file missing/corrupt, don't crash
                return new List<Product>();
            }
        }

        public void Save(List<Product> products)
        {
            try
            {
                string json = JsonConvert.SerializeObject(products, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not save file: " + ex.Message);
            }
        }
    }
}
