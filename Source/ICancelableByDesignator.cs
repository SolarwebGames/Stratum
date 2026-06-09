namespace SolarWeb.Stratum;

public interface ICancelableByDesignator
{
  bool CanCancel { get; }
  void CancelByDesignator();
}
