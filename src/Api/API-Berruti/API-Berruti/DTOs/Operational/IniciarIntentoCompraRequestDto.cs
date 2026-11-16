using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public class IniciarIntentoCompraRequestDto : IValidatableObject
{
    [Required]
    [MinLength(1)]
    public List<SeleccionTramoCompraDto> Tramos { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tramos is null)
        {
            yield break;
        }

        for (var index = 0; index < Tramos.Count; index++)
        {
            var tramo = Tramos[index];
            if (tramo is null)
            {
                yield return new ValidationResult(
                    $"El tramo en la posición {index} es obligatorio.",
                    [$"Tramos[{index}]"]);
                continue;
            }

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(
                tramo,
                new ValidationContext(tramo),
                results,
                validateAllProperties: true);

            foreach (var result in results)
            {
                var members = result.MemberNames.Any()
                    ? result.MemberNames.Select(member => $"Tramos[{index}].{member}").ToArray()
                    : [$"Tramos[{index}]"];
                yield return new ValidationResult(result.ErrorMessage, members);
            }
        }
    }
}
