namespace API_Berruti.Models.Operational;

public sealed record PlanEmisionCompra(
    Compra Compra,
    IReadOnlyList<CompraServicio> Servicios,
    IReadOnlyList<Ticket> Tickets);
