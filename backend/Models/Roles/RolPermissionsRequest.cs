namespace Backend.Models.Requests.Roles
{
    public class RolPermissionsRequest
    {
        public List<int> PermissionIds { get; set; } = new();
    }
}
