/** The service name with the department (clinic) below it as a smaller gray line. */
export function ServiceLabel({ serviceName, departmentName }: { serviceName: string; departmentName: string }) {
  return (
    <span>
      {serviceName}
      <span className="block text-sm font-normal text-slate-600">{departmentName}</span>
    </span>
  )
}
