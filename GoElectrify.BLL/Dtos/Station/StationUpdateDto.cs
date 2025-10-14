﻿namespace GoElectrify.BLL.Dto.Station
{
    public class StationUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Status { get; set; }
    }
}
