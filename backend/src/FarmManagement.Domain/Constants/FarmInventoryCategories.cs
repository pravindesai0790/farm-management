namespace FarmManagement.Domain.Constants;

public sealed record FarmInventoryCategoryDefinition(
    Guid Id,
    string Name,
    string Code,
    string Description,
    string Examples,
    string Icon,
    int DisplayOrder);

public static class FarmInventoryCategories
{
    public static readonly Guid SeedsPlantingId = Guid.Parse("c1000000-0000-0000-0000-000000000001");
    public static readonly Guid FertilizersSoilId = Guid.Parse("c1000000-0000-0000-0000-000000000002");
    public static readonly Guid AgrochemicalsPestControlId = Guid.Parse("c1000000-0000-0000-0000-000000000003");
    public static readonly Guid FeedAnimalNutritionId = Guid.Parse("c1000000-0000-0000-0000-000000000004");
    public static readonly Guid VeterinaryAnimalHealthId = Guid.Parse("c1000000-0000-0000-0000-000000000005");
    public static readonly Guid FuelOilLubricantsId = Guid.Parse("c1000000-0000-0000-0000-000000000006");
    public static readonly Guid ToolsHandEquipmentId = Guid.Parse("c1000000-0000-0000-0000-000000000007");
    public static readonly Guid MachineryEquipmentPartsId = Guid.Parse("c1000000-0000-0000-0000-000000000008");
    public static readonly Guid IrrigationPlumbingId = Guid.Parse("c1000000-0000-0000-0000-000000000009");
    public static readonly Guid HarvestingStorageSuppliesId = Guid.Parse("c1000000-0000-0000-0000-000000000010");
    public static readonly Guid SafetyProtectiveGearId = Guid.Parse("c1000000-0000-0000-0000-000000000011");
    public static readonly Guid BuildingFencingId = Guid.Parse("c1000000-0000-0000-0000-000000000012");
    public static readonly Guid GeneralSuppliesId = Guid.Parse("c1000000-0000-0000-0000-000000000013");

    public static readonly IReadOnlyList<FarmInventoryCategoryDefinition> All =
    [
        new(
            SeedsPlantingId,
            "Seeds & Planting Materials",
            "SEEDS_PLANTING",
            "Seeds, saplings, tubers, seedlings, and inoculants.",
            "Seeds, saplings, tubers, seedlings, inoculants",
            "spa",
            1),
        new(
            FertilizersSoilId,
            "Fertilizers & Soil Amendments",
            "FERTILIZERS_SOIL",
            "Nitrogen, phosphorus, potassium (NPK), organic compost, lime, and micronutrients.",
            "NPK, organic compost, lime, micronutrients",
            "compost",
            2),
        new(
            AgrochemicalsPestControlId,
            "Agrochemicals & Pest Control",
            "AGROCHEMICALS_PEST_CONTROL",
            "Herbicides, insecticides, fungicides, and rodenticides.",
            "Herbicides, insecticides, fungicides, rodenticides",
            "pest_control",
            3),
        new(
            FeedAnimalNutritionId,
            "Feed & Animal Nutrition",
            "FEED_ANIMAL_NUTRITION",
            "Livestock feed, forage, grains, mineral blocks, and supplements.",
            "Livestock feed, forage, grains, mineral blocks, supplements",
            "pets",
            4),
        new(
            VeterinaryAnimalHealthId,
            "Veterinary & Animal Health",
            "VETERINARY_ANIMAL_HEALTH",
            "Vaccines, medicines, dewormers, first-aid supplies, and tagging tools.",
            "Vaccines, medicines, dewormers, first-aid supplies, tagging tools",
            "medical_services",
            5),
        new(
            FuelOilLubricantsId,
            "Fuel, Oil & Lubricants",
            "FUEL_OIL_LUBRICANTS",
            "Diesel, gasoline, engine oil, and hydraulic fluid.",
            "Diesel, gasoline, engine oil, hydraulic fluid",
            "local_gas_station",
            6),
        new(
            ToolsHandEquipmentId,
            "Tools & Hand Equipment",
            "TOOLS_HAND_EQUIPMENT",
            "Shovels, pruners, hoes, forks, and buckets.",
            "Shovels, pruners, hoes, forks, buckets",
            "handyman",
            7),
        new(
            MachineryEquipmentPartsId,
            "Machinery & Heavy Equipment Parts",
            "MACHINERY_EQUIPMENT_PARTS",
            "Tractor parts, belts, filters, tires, and harvester components.",
            "Tractor parts, belts, filters, tires, harvester components",
            "precision_manufacturing",
            8),
        new(
            IrrigationPlumbingId,
            "Irrigation & Plumbing",
            "IRRIGATION_PLUMBING",
            "Pipes, valves, sprinklers, drip tape, and fittings.",
            "Pipes, valves, sprinklers, drip tape, fittings",
            "water_drop",
            9),
        new(
            HarvestingStorageSuppliesId,
            "Harvesting & Storage Supplies",
            "HARVESTING_STORAGE_SUPPLIES",
            "Crates, bins, bags, twine, and cold storage items.",
            "Crates, bins, bags, twine, cold storage items",
            "inventory_2",
            10),
        new(
            SafetyProtectiveGearId,
            "Safety & Protective Gear",
            "SAFETY_PROTECTIVE_GEAR",
            "Gloves, respirators, eye protection, and first-aid.",
            "Gloves, respirators, eye protection, first-aid",
            "health_and_safety",
            11),
        new(
            BuildingFencingId,
            "Building & Fencing Materials",
            "BUILDING_FENCING",
            "Posts, wire, netting, lumber, and fasteners.",
            "Posts, wire, netting, lumber, fasteners",
            "fence",
            12),
        new(
            GeneralSuppliesId,
            "General & Cleaning Supplies",
            "GENERAL_SUPPLIES",
            "Disinfectants, sanitizers, cleaning and utility supplies.",
            "Disinfectants, sanitizers, utility supplies",
            "cleaning_services",
            13)
    ];
}
