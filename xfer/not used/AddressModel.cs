public class AddressModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AddressModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public EditAddressVM AddressVM { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Load dropdowns
        AddressVM.Countries = await _context.Countries
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync();

        AddressVM.States = await _context.States
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();

        AddressVM.ClimateZones = await _context.ClimateZones
            .Select(z => new SelectListItem { Value = z.Id.ToString(), Text = z.Name })
            .ToListAsync();

        AddressVM.KoppenClimateZones = await _context.KoppenClimateZones
            .Select(k => new SelectListItem { Value = k.Id.ToString(), Text = k.Name })
            .ToListAsync();

        // Load address if editing
        if (id.HasValue)
        {
            var address = await _context.ProjectAddresses
                .Include(a => a.Country)
                .Include(a => a.State)
                .Include(a => a.ClimateZone)
                .Include(a => a.KoppenClimateZone)
                .FirstOrDefaultAsync(a => a.Id == id.Value);

            if (address == null) return NotFound();

            AddressVM = new EditAddressVM
            {
                ProjectId = address.Id,
                AddressId = address.Id,
                Address1 = address.Address1,
                Address2 = address.Address2,
                City = address.City,
                PostalCode = address.PostalCode,
                CountryId = address.CountryId,
                StateId = address.StateId,
                ClimateZoneId = address.ClimateZoneId,
                KoppenClimateZoneId = address.KoppenClimateZoneId,
                HDD = address.HDD,
                CDD = address.CDD,
                WindowWallRatio = address.WindowWallRatio,
                Location = address.Location,
                DesignStrategyIds = address.DesignStrategyIds
            };
        }

        // Load climate zone detail for strategies
        if (AddressVM.KoppenClimateZoneId.HasValue)
        {
            AddressVM.ClimateZoneDetail = await _context.ClimateScoutClimateZones
                .Include(z => z.DesignStrategies)
                .FirstOrDefaultAsync(z => z.Id == AddressVM.KoppenClimateZoneId.Value);
        }

        return Page();
    }
}