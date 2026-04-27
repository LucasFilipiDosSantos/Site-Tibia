import { useState } from "react";
import AdminLayout from "@/components/lootera/AdminLayout";
import { toast } from "sonner";
import { Save } from "lucide-react";

const AdminSettings = () => {
  const [settings, setSettings] = useState({
    storeName: "Lootera",
    storeEmail: "looteratibia@gmail.com",
    supportPhone: "82 99974-9180",
    currency: "BRL",
    deliveryTime: "5",
    maintenanceMode: false,
    notifyNewOrders: true,
    notifyLowStock: true,
  });

  const handleSave = () => {
    toast.success("Configurações salvas com sucesso!");
  };

  return (
    <AdminLayout>
      <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Configurações</h1>

      <div className="max-w-2xl space-y-6">
        <div className="rounded-xl border border-border bg-card p-6">
          <h2 className="mb-4 font-display text-base font-semibold text-foreground">Geral</h2>
          <div className="space-y-4">
            <div>
              <label className="text-xs text-muted-foreground">Nome da loja</label>
              <input value={settings.storeName} onChange={(e) => setSettings({ ...settings, storeName: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground">E-mail</label>
              <input value={settings.storeEmail} onChange={(e) => setSettings({ ...settings, storeEmail: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground">WhatsApp de suporte</label>
              <input value={settings.supportPhone} onChange={(e) => setSettings({ ...settings, supportPhone: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-xs text-muted-foreground">Moeda</label>
                <select value={settings.currency} onChange={(e) => setSettings({ ...settings, currency: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary">
                  <option value="BRL">BRL (R$)</option>
                  <option value="USD">USD ($)</option>
                  <option value="EUR">EUR (€)</option>
                </select>
              </div>
              <div>
                <label className="text-xs text-muted-foreground">Tempo de entrega (min)</label>
                <input value={settings.deliveryTime} onChange={(e) => setSettings({ ...settings, deliveryTime: e.target.value })} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
              </div>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-6">
          <h2 className="mb-4 font-display text-base font-semibold text-foreground">Notificações</h2>
          <div className="space-y-4">
            {[
              { key: "notifyNewOrders" as const, label: "Notificar novos pedidos" },
              { key: "notifyLowStock" as const, label: "Alertar estoque baixo" },
            ].map(({ key, label }) => (
              <label key={key} className="flex items-center justify-between">
                <span className="text-sm text-foreground">{label}</span>
                <button
                  type="button"
                  onClick={() => setSettings({ ...settings, [key]: !settings[key] })}
                  className={`relative h-6 w-11 rounded-full transition-colors ${settings[key] ? "bg-primary" : "bg-muted"}`}
                >
                  <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-foreground transition-transform ${settings[key] ? "left-[22px]" : "left-0.5"}`} />
                </button>
              </label>
            ))}
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-6">
          <h2 className="mb-4 font-display text-base font-semibold text-foreground">Sistema</h2>
          <label className="flex items-center justify-between">
            <div>
              <span className="text-sm text-foreground">Modo manutenção</span>
              <p className="text-xs text-muted-foreground">Desativa a loja temporariamente</p>
            </div>
            <button
              type="button"
              onClick={() => setSettings({ ...settings, maintenanceMode: !settings.maintenanceMode })}
              className={`relative h-6 w-11 rounded-full transition-colors ${settings.maintenanceMode ? "bg-destructive" : "bg-muted"}`}
            >
              <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-foreground transition-transform ${settings.maintenanceMode ? "left-[22px]" : "left-0.5"}`} />
            </button>
          </label>
        </div>

        <button onClick={handleSave} className="flex items-center gap-2 rounded-lg bg-primary px-6 py-3 text-sm font-medium text-primary-foreground hover:bg-primary/90">
          <Save size={16} /> Salvar configurações
        </button>
      </div>
    </AdminLayout>
  );
};

export default AdminSettings;
